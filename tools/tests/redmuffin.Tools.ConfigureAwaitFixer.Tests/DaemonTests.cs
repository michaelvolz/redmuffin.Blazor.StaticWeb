namespace redmuffin.Tools.ConfigureAwaitFixer.Tests;

using System.Diagnostics;

using redmuffin.Tools.ConfigureAwaitFixer;

[Category("Daemon")]
public sealed class DaemonTests
{
    private static string FixerDll => ConfigureAwaitFixerFixture.FixerDll;

    [Test]
    public async Task should_fix_file_via_spawned_daemon()
    {
        using var env = new TestEnv();
        await CreateTestProjectAsync(env.ProjectDir).ConfigureAwait(false);
        var testFile = await CreateFileWithBareAwaitAsync(env.ProjectDir).ConfigureAwait(false);

        var (exitCode, stderr) = await RunFixClientAsync(testFile, env).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(0);
        var fixedText = await File.ReadAllTextAsync(testFile).ConfigureAwait(false);
        await Assert.That(fixedText).Contains(".ConfigureAwait(false)");
        await Assert.That(stderr).Contains("Fixed 1 await(s)");
        var log = await File.ReadAllTextAsync(env.LogPath).ConfigureAwait(false);
        await Assert.That(log).Contains("Daemon starting");
        await Assert.That(log).Contains("Request:");
    }

    [Test]
    public async Task should_reuse_warm_daemon_for_second_file()
    {
        using var env = new TestEnv();
        await CreateTestProjectAsync(env.ProjectDir).ConfigureAwait(false);
        var first = await CreateFileWithBareAwaitAsync(env.ProjectDir, "First.cs").ConfigureAwait(false);
        var second = await CreateFileWithBareAwaitAsync(env.ProjectDir, "Second.cs").ConfigureAwait(false);

        await Assert.That((await RunFixClientAsync(first, env).ConfigureAwait(false)).ExitCode).IsEqualTo(0);
        await Assert.That((await RunFixClientAsync(second, env).ConfigureAwait(false)).ExitCode).IsEqualTo(0);

        var log = await File.ReadAllTextAsync(env.LogPath).ConfigureAwait(false);
        await Assert.That(CountOccurrences(log, "Daemon starting")).IsEqualTo(1);
    }

    [Test]
    public async Task should_handle_concurrent_clients()
    {
        using var env = new TestEnv();
        await CreateTestProjectAsync(env.ProjectDir).ConfigureAwait(false);
        var files = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var file = Path.Combine(env.ProjectDir, $"Class{i}.cs");
            await File.WriteAllTextAsync(file, BareAwaitClass($"Class{i}")).ConfigureAwait(false);
            files.Add(file);
        }

        var results = await Task.WhenAll(files.Select(f => RunFixClientAsync(f, env))).ConfigureAwait(false);

        foreach (var (exitCode, _) in results)
            await Assert.That(exitCode).IsEqualTo(0);
        foreach (var file in files)
            await Assert.That(await File.ReadAllTextAsync(file).ConfigureAwait(false)).Contains(".ConfigureAwait(false)");
    }

    [Test]
    public async Task should_fail_loudly_when_no_csproj_found()
    {
        using var env = new TestEnv();
        var orphan = Path.Combine(env.Root, "Orphan.cs");
        await File.WriteAllTextAsync(orphan, "public class Orphan { }").ConfigureAwait(false);

        var (exitCode, stderr) = await RunFixClientAsync(orphan, env).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(stderr).Contains("FATAL");
        await Assert.That(stderr).Contains("No .csproj found");
    }

    [Test]
    public async Task should_recover_after_daemon_crash()
    {
        using var env = new TestEnv();
        await CreateTestProjectAsync(env.ProjectDir).ConfigureAwait(false);
        var first = await CreateFileWithBareAwaitAsync(env.ProjectDir, "First.cs").ConfigureAwait(false);
        var second = await CreateFileWithBareAwaitAsync(env.ProjectDir, "Second.cs").ConfigureAwait(false);

        var daemon = await StartDaemonAsync(env, idleSeconds: "30").ConfigureAwait(false);
        try
        {
            _ = daemon.StandardError.ReadToEndAsync();
            _ = daemon.StandardOutput.ReadToEndAsync();
            daemon.StandardInput.Close();

            await Assert.That((await RunFixClientAsync(first, env).ConfigureAwait(false)).ExitCode).IsEqualTo(0);

            daemon.Kill(entireProcessTree: true);
            await daemon.WaitForExitAsync().ConfigureAwait(false);

            await Assert.That((await RunFixClientAsync(second, env).ConfigureAwait(false)).ExitCode).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(second).ConfigureAwait(false)).Contains(".ConfigureAwait(false)");

            var log = await File.ReadAllTextAsync(env.LogPath).ConfigureAwait(false);
            await Assert.That(CountOccurrences(log, "Daemon starting")).IsEqualTo(2);
        }
        finally
        {
            if (!daemon.HasExited)
                daemon.Kill(entireProcessTree: true);
        }
    }

    [Test]
    public async Task should_exit_after_idle_timeout()
    {
        using var env = new TestEnv();
        var daemon = await StartDaemonAsync(env, idleSeconds: "2").ConfigureAwait(false);
        try
        {
            _ = daemon.StandardError.ReadToEndAsync();
            _ = daemon.StandardOutput.ReadToEndAsync();
            daemon.StandardInput.Close();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await daemon.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            await Assert.That(daemon.ExitCode).IsEqualTo(0);
            await Assert.That(await File.ReadAllTextAsync(env.LogPath).ConfigureAwait(false)).Contains("Idle timeout");
        }
        finally
        {
            if (!daemon.HasExited)
                daemon.Kill(entireProcessTree: true);
        }
    }

    [Test]
    public async Task should_succeed_silently_on_zero_violations()
    {
        using var env = new TestEnv();
        await CreateTestProjectAsync(env.ProjectDir).ConfigureAwait(false);
        var testFile = Path.Combine(env.ProjectDir, "Clean.cs");
        await File.WriteAllTextAsync(testFile, "public class Clean { }").ConfigureAwait(false);

        var (exitCode, stderr) = await RunFixClientAsync(testFile, env).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).DoesNotContain("FATAL");
        await Assert.That(await File.ReadAllTextAsync(testFile).ConfigureAwait(false)).IsEqualTo("public class Clean { }");
    }

    [Test]
    public async Task Parse_RecognizesFixAndDaemonModes()
    {
        using var temp = new TempDir();
        var sourceFile = Path.Combine(temp.Path, "Source.cs");

        var fixResult = Arguments.Parse(["--fix", sourceFile]);
        await Assert.That(fixResult).IsNotNull();
        await Assert.That(fixResult!.Mode).IsEqualTo(FixerMode.Fix);
        await Assert.That(fixResult.SingleFile).IsEqualTo(sourceFile);

        var daemonResult = Arguments.Parse(["--daemon"]);
        await Assert.That(daemonResult).IsNotNull();
        await Assert.That(daemonResult!.Mode).IsEqualTo(FixerMode.Daemon);
    }

    [Test]
    public async Task Parse_ReturnsNull_WhenFixFlagHasNoValue()
    {
        var result = Arguments.Parse(["--fix"]);

        await Assert.That(result).IsNull();
    }

    private static async Task<(int ExitCode, string Stderr)> RunFixClientAsync(string file, TestEnv env)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{FixerDll}\" --fix \"{file}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["CONFIGUREAWAITFIXER_INSTANCE"] = env.Instance;
        psi.Environment["CONFIGUREAWAITFIXER_LOG"] = env.LogPath;
        psi.Environment["CONFIGUREAWAITFIXER_IDLE_SECONDS"] = "30";

        using var proc = Process.Start(psi)!;
        var stderr = await proc.StandardError.ReadToEndAsync().ConfigureAwait(false);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            proc.Kill(entireProcessTree: true);
            throw;
        }

        return (proc.ExitCode, stderr);
    }

    private static async Task<Process> StartDaemonAsync(TestEnv env, string? idleSeconds = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{FixerDll}\" --daemon",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };
        psi.Environment["CONFIGUREAWAITFIXER_INSTANCE"] = env.Instance;
        psi.Environment["CONFIGUREAWAITFIXER_LOG"] = env.LogPath;
        if (idleSeconds is not null)
            psi.Environment["CONFIGUREAWAITFIXER_IDLE_SECONDS"] = idleSeconds;

        return Process.Start(psi)!;
    }

    private static async Task<string> CreateFileWithBareAwaitAsync(string dir, string name = "TestClass.cs")
    {
        var path = Path.Combine(dir, name);
        await File.WriteAllTextAsync(path, BareAwaitClass(Path.GetFileNameWithoutExtension(name))).ConfigureAwait(false);
        return path;
    }

    private static string BareAwaitClass(string className) => $$"""
        using System.Threading.Tasks;

        public class {{className}}
        {
            public async Task DoAsync()
            {
                await Task.CompletedTask;
            }
        }
        """;

    private static async Task CreateTestProjectAsync(string dir)
    {
        var csproj = Path.Combine(dir, "TestProject.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
            </Project>
            """).ConfigureAwait(false);

        // CA2007 is disabled by default in .NET 10 — explicitly enable it
        var editorconfig = Path.Combine(dir, ".editorconfig");
        await File.WriteAllTextAsync(editorconfig, """
            [*.cs]
            dotnet_diagnostic.CA2007.severity = warning
            """).ConfigureAwait(false);
    }

    private static int CountOccurrences(string text, string substring)
    {
        var count = 0;
        var i = 0;
        while ((i = text.IndexOf(substring, i, StringComparison.Ordinal)) != -1)
        {
            count++;
            i += substring.Length;
        }

        return count;
    }

    private static void KillDaemonForInstance(string logPath)
    {
        if (!File.Exists(logPath))
            return;

        var log = File.ReadAllText(logPath);

        const string marker = "Daemon starting (pid ";
        var index = 0;
        while ((index = log.IndexOf(marker, index, StringComparison.Ordinal)) != -1)
        {
            var pidStart = index + marker.Length;
            var pidEnd = log.IndexOf(')', pidStart);
            if (pidEnd > pidStart && int.TryParse(log.AsSpan(pidStart, pidEnd - pidStart), out var pid))
            {
                var daemon = Process.GetProcesses().FirstOrDefault(p => p.Id == pid);
                if (daemon is not null)
                {
                    using (daemon)
                    {
                        if (!daemon.HasExited)
                        {
                            daemon.Kill(entireProcessTree: true);
                            daemon.WaitForExit();
                        }
                    }
                }
            }

            index = pidEnd + 1;
        }
    }

    private sealed class TestEnv : IDisposable
    {
        public TestEnv()
        {
            Root = Path.Combine(Path.GetTempPath(), $"caf-daemon-test-{Guid.NewGuid():N}");
            ProjectDir = Path.Combine(Root, "proj");
            Directory.CreateDirectory(ProjectDir);
            Instance = $"test-{Guid.NewGuid():N}";
            LogPath = Path.Combine(Root, "daemon.log");
        }

        public string Root { get; }

        public string ProjectDir { get; }

        public string Instance { get; }

        public string LogPath { get; }

        public void Dispose()
        {
            // The client-spawned daemon holds this temp directory open via its
            // workspace and working set. Kill it deterministically first —
            // no timing-dependent retry — then delete once.
            KillDaemonForInstance(LogPath);

            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class TempDir : IDisposable
    {
        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"caf-args-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
