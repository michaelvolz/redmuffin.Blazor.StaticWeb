namespace redmuffin.Tools.QualityGates.Analysis;

using System.Diagnostics;

public static class MutationRunner
{
    public static async Task<IReadOnlyList<MutantResult>> RunAsync(
        string sourcePath,
        IReadOnlyList<MutationSite> sites,
        string testProjectPath,
        int timeoutFactor = 10,
        string? testFilter = null,
        CancellationToken cancellationToken = default)
    {
        var (canProceed, timeout) = await RunBaselineOrEmptyAsync(
                testProjectPath, timeoutFactor, cancellationToken)
            .ConfigureAwait(false);
        if (!canProceed) return [];

        var originalSource = await File.ReadAllTextAsync(sourcePath, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var results = new List<MutantResult>();

            foreach (var site in sites)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mutated = MutationApplicator.Apply(originalSource, site);
                // Unchanged text: never run tests (would be a false "survived").
                if (ClassifyAfterApply(originalSource, mutated, testsPassed: false)
                    is MutantResultType.NoOp)
                {
                    results.Add(new MutantResult(
                        site.Index, site.Category, site.Line, site.Description,
                        MutantResultType.NoOp, DurationMs: 0));
                    continue;
                }

                await File.WriteAllTextAsync(sourcePath, mutated, cancellationToken)
                    .ConfigureAwait(false);

                var testResult = await RunTestsAsync(
                        testProjectPath, timeout, cancellationToken, testFilter)
                    .ConfigureAwait(false);
                results.Add(new MutantResult(
                    site.Index, site.Category, site.Line, site.Description,
                    ClassifyAfterApply(originalSource, mutated, testResult.Passed),
                    testResult.DurationMs));

                await File.WriteAllTextAsync(sourcePath, originalSource, cancellationToken)
                    .ConfigureAwait(false);
            }

            return results.AsReadOnly();
        }
        finally
        {
            // Restore source on any failure path
            if (File.Exists(sourcePath))
            {
                var current = await File.ReadAllTextAsync(
                        sourcePath, CancellationToken.None).ConfigureAwait(false);
                if (!string.Equals(current, originalSource, StringComparison.Ordinal))
                {
                    await File.WriteAllTextAsync(
                        sourcePath, originalSource, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }

    public static async Task<(bool CanProceed, long Timeout)> RunBaselineOrEmptyAsync(
        string testProjectPath, int timeoutFactor, CancellationToken cancellationToken = default)
    {
        var baselineResult = await RunTestsAsync(testProjectPath, timeout: null, cancellationToken)
            .ConfigureAwait(false);
        if (!baselineResult.Passed)
        {
            return (false, 0);
        }

        return (true, baselineResult.DurationMs * timeoutFactor);
    }

    private static async Task<TestRunResult> RunTestsAsync(
        string projectPath,
        long? timeout,
        CancellationToken cancellationToken,
        string? testFilter = null)
    {
        var arguments = $"run --project \"{projectPath}\" -p:TreatWarningsAsErrors=false";
        if (testFilter is not null)
        {
            arguments += $" -- --treenode-filter \"/*/*/{testFilter}/*\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        var sw = Stopwatch.StartNew();
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        if (timeout.HasValue)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken)
                    .WaitAsync(TimeSpan.FromMilliseconds(timeout.Value), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // Kill the process tree on timeout — never leak orphaned dotnet processes
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await outputTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);
                sw.Stop();

                return new TestRunResult(false, sw.ElapsedMilliseconds);
            }
        }
        else
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }

        await outputTask.ConfigureAwait(false);
        await errorTask.ConfigureAwait(false);

        sw.Stop();

        var passed = process.ExitCode == 0;

        return new TestRunResult(passed, sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Maps Apply output + test outcome to a result type.
    /// Unchanged source is always NoOp (tests never ran or would be meaningless).
    /// </summary>
    public static MutantResultType ClassifyAfterApply(
        string originalSource, string mutatedSource, bool testsPassed)
    {
        if (string.Equals(originalSource, mutatedSource, StringComparison.Ordinal))
            return MutantResultType.NoOp;

        return testsPassed ? MutantResultType.Survived : MutantResultType.Killed;
    }

    private sealed record TestRunResult(bool Passed, long DurationMs);
}
