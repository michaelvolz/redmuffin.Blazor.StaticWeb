using System.Collections.Immutable;
using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     The ConfigureAwaitFixer daemon: serves <c>--fix</c> requests over the
///     named pipe, keeps the MSBuildWorkspace and its projects warm in memory
///     (the RAM cache), and exits after 15 minutes without a request
///     (<c>CONFIGUREAWAITFIXER_IDLE_SECONDS</c> overrides this for tests).
///     All workspace mutations are serialized under one lock; analysis runs
///     concurrently on immutable compilation snapshots. Every request re-reads
///     the file from disk and re-checks it before writing, so the cache never
///     applies a stale fix or clobbers a concurrent edit.
/// </summary>
public sealed class Daemon : IAsyncDisposable
{
    private const int MaxConcurrentRequests = 8;
    private const int IdlePollIntervalSeconds = 1;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    private readonly MSBuildWorkspace _workspace = MSBuildWorkspace.Create();
    private readonly SemaphoreSlim _workspaceLock = new(1, 1);
    private readonly Dictionary<string, string> _syncedTexts = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, ProjectId> _projectIds = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly WorkspaceEventRegistration _workspaceFailedRegistration;
    private DateTime _lastActivityUtc = DateTime.UtcNow;

    private Daemon(ImmutableArray<DiagnosticAnalyzer> analyzers)
    {
        _analyzers = analyzers;
        _workspaceFailedRegistration = _workspace.RegisterWorkspaceFailedHandler(e =>
            DaemonLog.Error($"Workspace: {e.Diagnostic.Message}")
        );
    }

    /// <summary>
    ///     Starts the daemon and returns the process exit code (0 on idle
    ///     shutdown, 1 when the daemon cannot start). Daemon options passed by
    ///     a detached spawner (<c>--instance</c>, <c>--log</c>, <c>--idle</c>)
    ///     are applied as in-process environment overrides, so all downstream
    ///     consumers (pipe name, log path, idle timeout) read them unchanged.
    /// </summary>
    public static async Task<int> RunAsync(Arguments args)
    {
        ApplyEnvironmentOverrides(args);
        DaemonLog.Info(
            $"Daemon starting (pid {Environment.ProcessId.ToString(CultureInfo.InvariantCulture)})"
        );
        var analyzers = AnalyzerLoader.Load(DaemonLog.Error);
        if (analyzers.IsEmpty)
        {
            DaemonLog.Error("FATAL: No DiagnosticAnalyzer types found in analyzer DLL.");
            return 1;
        }

        var daemon = new Daemon(analyzers);
        await using (daemon.ConfigureAwait(false))
        {
            return await daemon.RunAsyncCoreAsync().ConfigureAwait(false);
        }
    }

    private static void ApplyEnvironmentOverrides(Arguments args)
    {
        if (!string.IsNullOrEmpty(args.Instance))
            Environment.SetEnvironmentVariable(Protocol.InstanceEnvVar, args.Instance);
        if (!string.IsNullOrEmpty(args.LogPath))
            Environment.SetEnvironmentVariable(Protocol.LogEnvVar, args.LogPath);
        if (!string.IsNullOrEmpty(args.IdleSeconds))
            Environment.SetEnvironmentVariable(Protocol.IdleEnvVar, args.IdleSeconds);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _shutdownCts.CancelAsync().ConfigureAwait(false);
        _workspace.Dispose();
        _workspaceLock.Dispose();
        _shutdownCts.Dispose();
    }

    private async Task<int> RunAsyncCoreAsync()
    {
        _lastActivityUtc = DateTime.UtcNow;
        var idleTimeout = GetIdleTimeout();
        DaemonLog.Info(
            $"Listening on pipe {Protocol.PipeName} (idle exit after {idleTimeout.ToString("c", CultureInfo.InvariantCulture)})"
        );

        var acceptLoop = AcceptLoopAsync();

        await IdleWatcherAsync(idleTimeout).ConfigureAwait(false);

        DaemonLog.Info("Idle timeout reached — shutting down.");
        await _shutdownCts.CancelAsync().ConfigureAwait(false);
        try
        {
            await acceptLoop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected: the accept loop exits via the shutdown token we just
            // cancelled. Record it so the shutdown sequence is fully visible
            // in the log.
            DaemonLog.Info("Accept loop stopped (shutdown).");
        }

        DaemonLog.Info("Daemon exited.");
        return 0;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_shutdownCts.IsCancellationRequested)
        {
            var client = new NamedPipeServerStream(
                Protocol.PipeName,
                PipeDirection.InOut,
                MaxConcurrentRequests,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0
            );

            try
            {
                await client.WaitForConnectionAsync(_shutdownCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await client.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                DaemonLog.Error($"Pipe accept failed: {ex.Message}");
                await client.DisposeAsync().ConfigureAwait(false);
                if (_shutdownCts.IsCancellationRequested)
                    return;
                await Task.Delay(250).ConfigureAwait(false);
                continue;
            }

            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream client)
    {
        _lastActivityUtc = DateTime.UtcNow;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token);
            cts.CancelAfter(RequestTimeout);

            var json = await Protocol.ReadAsync(client, cts.Token).ConfigureAwait(false);
            var request =
                JsonSerializer.Deserialize<Protocol.FixRequest>(json, Protocol.JsonOptions)
                ?? throw new InvalidDataException("Malformed daemon request.");

            DaemonLog.Info($"Request: {request.File}");
            var response = await HandleRequestAsync(request.File, cts.Token).ConfigureAwait(false);
            DaemonLog.Info(
                response.Ok
                    ? $"Responded: {response.Message}"
                    : $"Failure response: {response.Message}"
            );

            await Protocol
                .WriteAsync(
                    client,
                    JsonSerializer.Serialize(response, Protocol.JsonOptions),
                    cts.Token
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await TryRespondFailureAsync(client, "Daemon request timed out.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DaemonLog.Error($"FATAL: {ex}");
            await TryRespondFailureAsync(client, $"Daemon error: {ex.Message}")
                .ConfigureAwait(false);
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task TryRespondFailureAsync(NamedPipeServerStream client, string message)
    {
        try
        {
            await Protocol
                .WriteAsync(
                    client,
                    JsonSerializer.Serialize(Protocol.Failure(message), Protocol.JsonOptions),
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DaemonLog.Error($"Could not send failure response: {ex.Message}");
        }
    }

    private async Task<Protocol.FixResponse> HandleRequestAsync(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        var fullPath = Path.GetFullPath(filePath);

        if (!SyntaxFixer.IsSourceFile(fullPath))
            return Protocol.Success(0, string.Empty);

        if (!File.Exists(fullPath))
            return Protocol.Success(0, string.Empty);

        var projectPath = Arguments.FindCsproj(Path.GetDirectoryName(fullPath)!);
        if (projectPath is null)
            return Protocol.Failure($"No .csproj found above {Path.GetDirectoryName(fullPath)}");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var originalText = await File.ReadAllTextAsync(fullPath, cancellationToken)
                .ConfigureAwait(false);
            var diagnostics = await GetDiagnosticsAsync(
                    projectPath,
                    fullPath,
                    originalText,
                    cancellationToken
                )
                .ConfigureAwait(false);
            var outcome = FixRunner.ApplyFixes(diagnostics, fullPath, originalText);

            if (outcome.ParseError is not null)
                return Protocol.Failure(
                    $"Parse error after fix in {fullPath}: {outcome.ParseError}"
                );

            if (outcome.NewText is null)
                return Protocol.Success(0, string.Empty);

            // Pre-write re-check: the file may have changed while we analyzed.
            // Retry against the new content; never clobber concurrent edits.
            var currentText = await File.ReadAllTextAsync(fullPath, cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(currentText, originalText, StringComparison.Ordinal))
                continue;

            await File.WriteAllTextAsync(fullPath, outcome.NewText, cancellationToken)
                .ConfigureAwait(false);
            var fixedMessage =
                $"Fixed {outcome.FixedAwaits.ToString(CultureInfo.InvariantCulture)} await(s) in {fullPath}";
            DaemonLog.Info(fixedMessage);
            return Protocol.Success(outcome.FixedAwaits, fixedMessage);
        }

        return Protocol.Failure(
            $"File {fullPath} kept changing while it was analyzed; retry the edit."
        );
    }

    private async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string projectPath,
        string filePath,
        string originalText,
        CancellationToken cancellationToken
    )
    {
        await _workspaceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        ProjectId projectId;
        try
        {
            projectId = await EnsureProjectOpenAsync(projectPath, cancellationToken)
                .ConfigureAwait(false);
            await SyncDocumentAsync(
                    projectId,
                    projectPath,
                    filePath,
                    originalText,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            _workspaceLock.Release();
        }

        var project =
            _workspace.CurrentSolution.GetProject(projectId)
            ?? throw new InvalidOperationException($"Workspace lost project {projectPath}.");
        var compilation =
            await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Failed to get compilation for {projectPath}.");

        var diagnostics = await compilation
            .WithAnalyzers(_analyzers)
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return diagnostics
            .Where(d =>
                string.Equals(d.Id, "CA2007", StringComparison.Ordinal)
                && d.Location.SourceTree is not null
                && string.Equals(
                    d.Location.SourceTree.FilePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToImmutableArray();
    }

    private async Task<ProjectId> EnsureProjectOpenAsync(
        string projectPath,
        CancellationToken cancellationToken
    )
    {
        if (
            _projectIds.TryGetValue(projectPath, out var cached)
            && _workspace.CurrentSolution.GetProject(cached) is not null
        )
        {
            return cached;
        }

        // MSBuildWorkspace opens the whole project graph, so a project we have
        // only seen as a reference (e.g. a library pulled in by an opened tests
        // project) is already part of the workspace solution. Re-opening it
        // throws "'<project>' is already part of the workspace" — reuse the
        // already-open project instead.
        var existing = FindOpenProject(projectPath);
        if (existing is not null)
        {
            _projectIds[projectPath] = existing.Id;
            return existing.Id;
        }

        DaemonLog.Info($"Opening project {projectPath}");
        var project = await _workspace
            .OpenProjectAsync(projectPath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _projectIds[projectPath] = project.Id;
        return project.Id;
    }

    /// <summary>
    ///     Returns the project already part of the workspace solution whose
    ///     csproj path matches <paramref name="projectPath"/>, or null. The
    ///     workspace loads whole project graphs, so a project we have only seen
    ///     as a reference is present here even though we never opened it
    ///     explicitly; re-opening it throws "'&lt;project&gt;' is already part
    ///     of the workspace".
    /// </summary>
    private Project? FindOpenProject(string projectPath) =>
        _workspace.CurrentSolution.Projects.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.FilePath)
            && string.Equals(p.FilePath, projectPath, StringComparison.OrdinalIgnoreCase)
        );

    private async Task SyncDocumentAsync(
        ProjectId projectId,
        string projectPath,
        string filePath,
        string originalText,
        CancellationToken cancellationToken
    )
    {
        if (
            _syncedTexts.TryGetValue(filePath, out var synced)
            && string.Equals(synced, originalText, StringComparison.Ordinal)
        )
        {
            return;
        }

        var solution = _workspace.CurrentSolution;
        var project =
            solution.GetProject(projectId)
            ?? throw new InvalidOperationException($"Workspace lost project {projectId}.");
        var document = project.Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase)
        );

        var newSolution = document is not null
            ? solution.WithDocumentText(document.Id, SourceText.From(originalText, Encoding.UTF8))
            : solution.AddDocument(
                DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(filePath),
                    filePath: filePath,
                    loader: TextLoader.From(
                        TextAndVersion.Create(
                            SourceText.From(originalText, Encoding.UTF8),
                            VersionStamp.Create()
                        )
                    )
                )
            );

        if (_workspace.TryApplyChanges(newSolution))
        {
            _syncedTexts[filePath] = originalText;
            return;
        }

        var reloadedText = await ReloadProjectAsync(
                projectId,
                projectPath,
                filePath,
                cancellationToken
            )
            .ConfigureAwait(false);
        _syncedTexts[filePath] = reloadedText;
    }

    /// <summary>
    ///     MSBuildWorkspace rejected the in-memory change (typically a
    ///     brand-new file that is not yet part of the evaluated project).
    ///     Reload the project from disk so the file is visible, and return
    ///     the text the workspace actually holds.
    /// </summary>
    private async Task<string> ReloadProjectAsync(
        ProjectId projectId,
        string projectPath,
        string filePath,
        CancellationToken cancellationToken
    )
    {
        DaemonLog.Info($"Workspace rejected sync for {filePath}; reloading {projectPath}");
        _workspace.TryApplyChanges(_workspace.CurrentSolution.RemoveProject(projectId));
        _projectIds.Remove(projectPath);

        var reopened = await _workspace
            .OpenProjectAsync(projectPath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var reopenedDocument = reopened.Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase)
        );
        if (reopenedDocument is null)
        {
            throw new InvalidOperationException(
                $"File {filePath} is not part of project {projectPath}; add it to the project before fixing."
            );
        }

        return (
            await reopenedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)
        ).ToString();
    }

    private async Task IdleWatcherAsync(TimeSpan idleTimeout)
    {
        while (!_shutdownCts.IsCancellationRequested)
        {
            if (DateTime.UtcNow - _lastActivityUtc >= idleTimeout)
                return;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(IdlePollIntervalSeconds), _shutdownCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static TimeSpan GetIdleTimeout()
    {
        var raw = Environment.GetEnvironmentVariable("CONFIGUREAWAITFIXER_IDLE_SECONDS");
        if (
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            && seconds > 0
        )
            return TimeSpan.FromSeconds(seconds);
        return TimeSpan.FromMinutes(15);
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:28.6858589Z","moduleHash":"91e139d78f4aadb91b52d240a139c1970fb7a7b8db678ad222bcb65b8d98b8ea","forms":[{"id":"RunAsync","line":48,"endLine":63,"hash":"1f4e9cb2054d8a047713ab686f1de7b0309bbca75dcd8403567085d299c4efd9"},{"id":"DisposeAsync","line":66,"endLine":72,"hash":"4e9cd45a9cd16e12279d2468a1b0d92d374058b49df406cd4a4a82d95990b410"},{"id":"RunAsyncCoreAsync","line":74,"endLine":100,"hash":"928c7cf73d5c1efc01b484836047d6bc2ef472643da08720039fa676162972f4"},{"id":"AcceptLoopAsync","line":102,"endLine":136,"hash":"21575797928d1a07014127eda1caa3dfedde63495ad0a1b4455b4e631907726d"},{"id":"HandleClientAsync","line":138,"endLine":170,"hash":"11db63e78893d406a97d8b40fa6400beb4a63e523d9b824a9b20ad289eff0de8"},{"id":"TryRespondFailureAsync","line":172,"endLine":186,"hash":"d34c9436b0b52e885c088d209f1438cc4bbb1eeb3301e4672fde68d1ea9a5c37"},{"id":"HandleRequestAsync","line":188,"endLine":228,"hash":"d5f3d7169bc8b47a2b5ef60bafdc3fef3d97883b349cec2034f7809dc6f7d6e5"},{"id":"GetDiagnosticsAsync","line":230,"endLine":263,"hash":"8d6f7c0fd8ed946740062d215a269cdae266cd22e8b66d3858af280d522b56f6"},{"id":"EnsureProjectOpenAsync","line":265,"endLine":278,"hash":"90a0fbfde29406f73b48e7ad6fd328fd6f8fe8cd5a01c735b1346fa1ee8f9181"},{"id":"SyncDocumentAsync","line":280,"endLine":335,"hash":"a807743236da79786146c48844f27bf544fb1f41625e918b6383651ccaf5a6ad"},{"id":"IdleWatcherAsync","line":337,"endLine":353,"hash":"da7025771c2d1e0d28a2836e28d622cc6158ab7239c73bf5a203db92331b7498"},{"id":"GetIdleTimeout","line":355,"endLine":361,"hash":"b16bab60e741c1f50bf95588c16608fbbf8577e357b77fe2e14d6ba4aaa15bae"}]}
// clj-mutate-manifest-end
