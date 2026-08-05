using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     The legacy one-shot pipeline (positional target directory or
///     <c>--file &lt;path&gt;</c>): opens MSBuildWorkspace, fixes all CA2007
///     violations, and exits. Behavior is preserved verbatim for OpenCode.
/// </summary>
public static class OneShotRunner
{
    /// <summary>
    ///     Runs the one-shot pipeline and returns the process exit code.
    /// </summary>
    public static async Task<int> RunAsync(Arguments args)
    {
        var analyzers = AnalyzerLoader.Load();
        if (analyzers.IsEmpty)
        {
            await Console.Error.WriteLineAsync("No DiagnosticAnalyzer types found in analyzer DLL.").ConfigureAwait(false);
            return 1;
        }

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(args.ProjectPath).ConfigureAwait(false);
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        if (compilation is null)
        {
            await Console.Error.WriteLineAsync($"Failed to get compilation for {args.ProjectPath}").ConfigureAwait(false);
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();

        var diagnostics = (await compilation
                .WithAnalyzers(analyzers)
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false))
            .Where(d => string.Equals(d.Id, "CA2007", StringComparison.Ordinal))
            .ToList();

        await ApplyFixesAsync(args, diagnostics).ConfigureAwait(false);

        stopwatch.Stop();
        await Console.Error.WriteLineAsync(
            $"[ConfigureAwaitFixer] Completed in {stopwatch.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s")
            .ConfigureAwait(false);

        return 0;
    }

    private static async Task ApplyFixesAsync(Arguments args, List<Diagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
            return;

        // Group diagnostics by file and apply fixes
        var diagnosticsByFile = diagnostics
            .Where(d => d.Location.SourceTree is not null
                && SyntaxFixer.IsSourceFile(d.Location.SourceTree.FilePath)
                && (args.SingleFile is null
                    || string.Equals(d.Location.SourceTree.FilePath, args.SingleFile, StringComparison.Ordinal)))
            .GroupBy(d => d.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ToList();

        var totalFilesFixed = 0;
        var totalAwaitsFixed = 0;

        foreach (var fileGroup in diagnosticsByFile)
        {
            var (filesFixed, awaitsFixed) = await FixFileGroupAsync(fileGroup).ConfigureAwait(false);
            totalFilesFixed += filesFixed;
            totalAwaitsFixed += awaitsFixed;
        }

        if (totalFilesFixed > 0)
        {
            await Console.Error.WriteLineAsync(
                $"ConfigureAwaitFixer: {totalAwaitsFixed.ToString(CultureInfo.InvariantCulture)} await(s) fixed in {totalFilesFixed.ToString(CultureInfo.InvariantCulture)} file(s)")
                .ConfigureAwait(false);
        }
    }

    private static async Task<(int FilesFixed, int AwaitsFixed)> FixFileGroupAsync(IGrouping<string, Diagnostic> fileGroup)
    {
        var originalText = await File.ReadAllTextAsync(fileGroup.Key).ConfigureAwait(false);
        var outcome = FixRunner.ApplyFixes(fileGroup, fileGroup.Key, originalText);

        if (outcome.ParseError is not null)
        {
            await Console.Error.WriteLineAsync(
                $"Parse error after fix in {fileGroup.Key}: {outcome.ParseError} — skipping write")
                .ConfigureAwait(false);
            return (0, -fileGroup.Count());
        }

        if (outcome.NewText is null)
            return (0, 0);

        await File.WriteAllTextAsync(fileGroup.Key, outcome.NewText).ConfigureAwait(false);
        await Console.Error.WriteLineAsync(
            $"Fixed {fileGroup.Count().ToString(CultureInfo.InvariantCulture)} await(s) in {fileGroup.Key}").ConfigureAwait(false);
        return (1, outcome.FixedAwaits);
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:29.1076577Z","moduleHash":"70011096a6c6704a1f3cc0121a2e4beeea73a99b13d8f605c6e2173ecd65320e","forms":[{"id":"RunAsync","line":18,"endLine":53,"hash":"ec4c3cf1860d51123c51a5d4762a0a393d76adcbb1b99f26c23ddd7e37cc8605"},{"id":"ApplyFixesAsync","line":55,"endLine":85,"hash":"87bdc9b2e1a3b4ae5e2264480bc7085da1dc0b14f284ac5d4117ff7226299c0f"},{"id":"FixFileGroupAsync","line":87,"endLine":107,"hash":"8c85563b6ccfe20e97809c4324dc1d2f1dc261203d9234eca4bee6fe461d9783"}]}
// clj-mutate-manifest-end
