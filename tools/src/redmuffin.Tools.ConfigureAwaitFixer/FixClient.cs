using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     The <c>--fix</c> client: hands a single file to the daemon and fails
///     loudly (stderr + exit 1) whenever the daemon is unreachable or
///     misbehaves. There is deliberately no fallback to the one-shot pipeline:
///     a broken daemon is a bug we must see, not paper over. The daemon is
///     spawned on demand under a named-mutex claim so concurrent clients never
///     double-spawn. Spawn is detached via Windows Task Scheduler so the
///     daemon is not a child of the harness Job Object and survives the hook.
/// </summary>
public static class FixClient
{
    private static readonly TimeSpan ConnectAttemptTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SpawnClaimTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Hard wall budget for one <c>--fix</c> invocation (connect + spawn
    ///     wait + request). Must finish under the PostToolUse orchestrator
    ///     (30 s) and the Host formatter budget (~25 s).
    /// </summary>
    private static readonly TimeSpan ClientBudget = TimeSpan.FromSeconds(22);

    /// <summary>
    ///     Runs the client for one file and returns the process exit code.
    /// </summary>
    public static async Task<int> RunAsync(string filePath)
    {
        try
        {
            var (ok, message) = await SendRequestAsync(filePath).ConfigureAwait(false);
            if (ok)
            {
                if (message.Length > 0)
                    await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
                return 0;
            }

            await Console.Error.WriteLineAsync($"FATAL: {message}").ConfigureAwait(false);
            DaemonLog.Error($"FATAL (client {Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}): {message}");
            return 1;
        }
        catch (Exception ex)
        {
            var fatal = $"FATAL: ConfigureAwaitFixer daemon failed: {ex.Message}";
            await Console.Error.WriteLineAsync(fatal).ConfigureAwait(false);
            DaemonLog.Error(fatal);
            return 1;
        }
    }

    private static async Task<(bool Ok, string Message)> SendRequestAsync(string filePath)
    {
        using var budget = new CancellationTokenSource(ClientBudget);
        var pipe = await ConnectWithArbitrationAsync(budget.Token).ConfigureAwait(false);
        await using (pipe.ConfigureAwait(false))
        {
            var request = JsonSerializer.Serialize(new Protocol.FixRequest(filePath), Protocol.JsonOptions);
            await Protocol.WriteAsync(pipe, request, budget.Token).ConfigureAwait(false);
            var json = await Protocol.ReadAsync(pipe, budget.Token).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<Protocol.FixResponse>(json, Protocol.JsonOptions)
                ?? throw new InvalidDataException("Daemon returned an unparseable response.");
            return (response.Ok, response.Message);
        }
    }

    private static async Task<NamedPipeClientStream> ConnectWithArbitrationAsync(CancellationToken budget)
    {
        var spawned = false;

        while (true)
        {
            budget.ThrowIfCancellationRequested();

            if (await TryConnectAsync(budget).ConfigureAwait(false) is { } pipe)
                return pipe;

            if (!spawned && TryClaimSpawn())
            {
                SpawnDaemonDetached();
                spawned = true;
            }

            await Task.Delay(200, budget).ConfigureAwait(false);
        }
    }

    private static bool TryClaimSpawn()
    {
        using var mutex = new Mutex(false, Protocol.MutexName);
        var acquired = false;
        try
        {
            acquired = mutex.WaitOne(SpawnClaimTimeout);
        }
        catch (AbandonedMutexException)
        {
            // A previous spawner died while holding the mutex; the mutex now
            // belongs to this thread, so treat the claim as successful.
            acquired = true;
        }

        if (!acquired)
            return false;

        try
        {
            // A daemon may have appeared while we waited for the mutex. The
            // check is synchronous so that acquire and release happen on the
            // same thread — ReleaseMutex from another thread would throw.
            return !TryConnectWithin(ConnectAttemptTimeout);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static bool TryConnectWithin(TimeSpan timeout)
    {
        var pipe = new NamedPipeClientStream(".", Protocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            pipe.Connect((int)timeout.TotalMilliseconds);
            pipe.Dispose();
            return true;
        }
        catch
        {
            pipe.Dispose();
            return false;
        }
    }

    private static async Task<NamedPipeClientStream?> TryConnectAsync(CancellationToken budget)
    {
        var pipe = new NamedPipeClientStream(".", Protocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(budget);
            attempt.CancelAfter(ConnectAttemptTimeout);
            await pipe.ConnectAsync(attempt.Token).ConfigureAwait(false);
            return pipe;
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    ///     Starts the daemon outside the current process tree / Job Object.
    ///     Windows Task Scheduler parents the process to the Task Scheduler
    ///     service, so a harness Job Object kill of this client does not kill
    ///     the warm daemon. Instance, log path, and idle timeout are passed as
    ///     explicit CLI args (env vars do not cross the scheduler boundary).
    /// </summary>
    private static void SpawnDaemonDetached()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "ConfigureAwaitFixer daemon auto-spawn requires Windows Task Scheduler.");
        }

        var (executable, arguments) = BuildDaemonLaunch();
        TaskSchedulerSpawn.RunDetached(executable, arguments, AppContext.BaseDirectory);
        DaemonLog.Info(
            $"Detached daemon spawn requested via Task Scheduler (exe={executable}).");
    }

    private static (string Executable, string Arguments) BuildDaemonLaunch()
    {
        var entry = Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "Cannot resolve the current process path to spawn the daemon.");

        var isDotnetHost = string.Equals(
            Path.GetFileNameWithoutExtension(entry), "dotnet", StringComparison.OrdinalIgnoreCase);

        // ProcessPath is always absolute; required so Task Scheduler can find it.
        var executable = entry;
        var args = new StringBuilder();
        if (isDotnetHost)
        {
            args.Append('"');
            args.Append(Path.Combine(AppContext.BaseDirectory, "ConfigureAwaitFixer.dll"));
            args.Append("\" ");
        }

        args.Append("--daemon");
        AppendForwardedArg(args, "--instance", Environment.GetEnvironmentVariable(Protocol.InstanceEnvVar));
        AppendForwardedArg(args, "--log", Environment.GetEnvironmentVariable(Protocol.LogEnvVar));
        AppendForwardedArg(args, "--idle", Environment.GetEnvironmentVariable(Protocol.IdleEnvVar));

        return (executable, args.ToString());
    }

    private static void AppendForwardedArg(StringBuilder args, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        args.Append(' ');
        args.Append(name);
        args.Append(" \"");
        args.Append(value.Replace("\"", "\\\"", StringComparison.Ordinal));
        args.Append('"');
    }

    /// <summary>
    ///     Demand-starts a process via the Windows Task Scheduler 2.0 COM API
    ///     so it is not born inside the caller's Job Object. Registers a hidden
    ///     one-shot task, runs it, then deletes the task definition. Failures
    ///     throw — there is no fallback to in-job Process.Start.
    /// </summary>
    private static class TaskSchedulerSpawn
    {
        private const int TaskActionExec = 0;
        private const int TaskCreateOrUpdate = 6;
        private const int TaskLogonInteractiveToken = 3;
        private const int TaskRunlevelLua = 0;

        public static void RunDetached(string executable, string arguments, string workingDirectory)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "ConfigureAwaitFixer daemon auto-spawn requires Windows Task Scheduler.");
            }

            RunDetachedWindows(executable, arguments, workingDirectory);
        }

        [SupportedOSPlatform("windows")]
        private static void RunDetachedWindows(string executable, string arguments, string workingDirectory)
        {
            var taskName = "redmuffin-ConfigureAwaitFixer-" + Guid.NewGuid().ToString("N");

            var schedulerType = Type.GetTypeFromProgID("Schedule.Service")
                ?? throw new InvalidOperationException(
                    "Windows Task Scheduler is unavailable (Schedule.Service ProgID).");

            dynamic service = Activator.CreateInstance(schedulerType)
                ?? throw new InvalidOperationException("Failed to create Schedule.Service.");

            service.Connect();
            dynamic folder = service.GetFolder("\\");
            dynamic definition = service.NewTask(0);

            definition.RegistrationInfo.Description =
                "One-shot detached spawn of the ConfigureAwaitFixer daemon (not a fixed service).";
            definition.Settings.Enabled = true;
            definition.Settings.Hidden = true;
            definition.Settings.AllowDemandStart = true;
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.StartWhenAvailable = true;
            definition.Principal.LogonType = TaskLogonInteractiveToken;
            definition.Principal.RunLevel = TaskRunlevelLua;

            dynamic action = definition.Actions.Create(TaskActionExec);
            action.Path = executable;
            action.Arguments = arguments;
            action.WorkingDirectory = workingDirectory;

            folder.RegisterTaskDefinition(
                taskName,
                definition,
                TaskCreateOrUpdate,
                null,
                null,
                TaskLogonInteractiveToken,
                null);

            try
            {
                dynamic task = folder.GetTask(taskName);
                _ = task.Run(null);
            }
            finally
            {
                try
                {
                    folder.DeleteTask(taskName, 0);
                }
                catch (Exception ex)
                {
                    // Task already ran; orphaned definition is annoying but not fatal.
                    Debug.WriteLine($"Task Scheduler cleanup failed for {taskName}: {ex.Message}");
                }
            }
        }
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:28.9634185Z","moduleHash":"eec25cbf6cf8163a7ad2f73b05145e225dcb0c983ecb6191ea9edaf94152dfb8","forms":[{"id":"RunAsync","line":27,"endLine":50,"hash":"b425af3d44ae519dd8065cbbe973bc840f5e2d4d0e93814c789445e8289afe14"},{"id":"SendRequestAsync","line":52,"endLine":65,"hash":"71dcc5c8f50630cf5942acd6270c03b9bf3c193324d6aebcec8bbbd1ac7fda69"},{"id":"ConnectWithArbitrationAsync","line":67,"endLine":89,"hash":"8010310e34b055ba46a751735b0104efdb6e70653566a22040d9b2560c8e3c4e"},{"id":"TryClaimSpawn","line":91,"endLine":120,"hash":"4ea8fb9cf654ba52f698912b1a0926481c2871785632a3ee58085c973a8bd332"},{"id":"TryConnectWithin","line":122,"endLine":136,"hash":"40f7952567951c9b5122aa09fee6ab496569cfd357e6c8feb12e5c83dea646e7"},{"id":"TryConnectAsync","line":138,"endLine":152,"hash":"80e9929365490738a8cbce4bb9bc4be0ab51aa6605733eddd8897e829aadb167"},{"id":"SpawnDaemon","line":154,"endLine":191,"hash":"5a05e635e624de5d5152b915393178bfce78ab9bcc34d985c6e9e028a294c62d"}]}
// clj-mutate-manifest-end
