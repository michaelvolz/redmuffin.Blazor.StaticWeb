namespace redmuffin.Tools.QualityGates.Analysis;

using System.Diagnostics;

public sealed record MutantResult(
    int SiteIndex,
    MutationCategory Category,
    int Line,
    string Description,
    MutantResultType Result,
    long DurationMs);

public enum MutantResultType
{
    Killed,
    Survived,
    Error,
}

public static class MutationRunner
{
    public static async Task<IReadOnlyList<MutantResult>> RunAsync(
        string sourcePath,
        IReadOnlyList<MutationSite> sites,
        string testProjectPath,
        int timeoutFactor = 10)
    {
        // 1. Run baseline
        var baselineResult = await RunTestsAsync(testProjectPath, timeout: null).ConfigureAwait(false);
        if (!baselineResult.Passed)
        {
            return Array.Empty<MutantResult>();
        }

        var timeout = baselineResult.DurationMs * timeoutFactor;

        // 2. Backup original source
        var backupPath = sourcePath + ".mutate-backup";
        var originalSource = await File.ReadAllTextAsync(sourcePath).ConfigureAwait(false);
        await File.WriteAllTextAsync(backupPath, originalSource).ConfigureAwait(false);

        try
        {
            var results = new List<MutantResult>();

            foreach (var site in sites)
            {
                // Read current source, apply mutation, write back
                var currentSource = await File.ReadAllTextAsync(sourcePath).ConfigureAwait(false);
                var mutated = MutationApplicator.Apply(currentSource, site.Index, site);
                await File.WriteAllTextAsync(sourcePath, mutated).ConfigureAwait(false);

                // Run tests
                var testResult = await RunTestsAsync(testProjectPath, timeout).ConfigureAwait(false);

                var resultType = testResult.Passed ? MutantResultType.Survived : MutantResultType.Killed;

                results.Add(new MutantResult(
                    site.Index,
                    site.Category,
                    site.Line,
                    site.Description,
                    resultType,
                    testResult.DurationMs));

                // Restore original source
                await File.WriteAllTextAsync(sourcePath, originalSource).ConfigureAwait(false);
            }

            return results.AsReadOnly();
        }
        finally
        {
            // Clean up backup
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    private static async Task<TestRunResult> RunTestsAsync(string projectPath, long? timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        var sw = Stopwatch.StartNew();
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (timeout.HasValue)
        {
            await process.WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromMilliseconds(timeout.Value)).ConfigureAwait(false);
        }
        else
        {
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // Await output/error tasks to prevent disposal race
        await outputTask.ConfigureAwait(false);
        await errorTask.ConfigureAwait(false);

        sw.Stop();

        // TUnit's dotnet run exits 0 on pass, non-zero on test failure
        var passed = process.ExitCode == 0;

        return new TestRunResult(passed, sw.ElapsedMilliseconds);
    }

    private sealed record TestRunResult(bool Passed, long DurationMs);
}
