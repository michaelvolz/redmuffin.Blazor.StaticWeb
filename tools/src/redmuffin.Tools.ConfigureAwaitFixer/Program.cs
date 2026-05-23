using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using redmuffin.Tools.ConfigureAwaitFixer;

if (Arguments.IsRunningInCI())
    return 0;

var args_ = Arguments.Parse(args);
if (args_ is null)
    return 1;

var analyzers = AnalyzerLoader.Load();
if (analyzers.IsEmpty)
{
    await Console.Error.WriteLineAsync("No DiagnosticAnalyzer types found in analyzer DLL.").ConfigureAwait(false);
    return 1;
}

using var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(args_.ProjectPath).ConfigureAwait(false);
var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
if (compilation is null)
{
    await Console.Error.WriteLineAsync($"Failed to get compilation for {args_.ProjectPath}").ConfigureAwait(false);
    return 1;
}

var stopwatch = Stopwatch.StartNew();

var diagnostics = (await compilation
        .WithAnalyzers(analyzers)
        .GetAnalyzerDiagnosticsAsync()
        .ConfigureAwait(false))
    .Where(d => string.Equals(d.Id, "CA2007", StringComparison.Ordinal))
    .ToList();

if (diagnostics.Count == 0)
{
    stopwatch.Stop();
    await Console.Error.WriteLineAsync(
        $"[ConfigureAwaitFixer] Completed in {stopwatch.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s")
        .ConfigureAwait(false);
    return 0;
}

// Group diagnostics by file and apply fixes
var diagnosticsByFile = diagnostics
    .Where(d => d.Location.SourceTree is not null
        && SyntaxFixer.IsSourceFile(d.Location.SourceTree.FilePath)
        && (args_.SingleFile is null
            || string.Equals(d.Location.SourceTree.FilePath, args_.SingleFile, StringComparison.Ordinal)))
    .GroupBy(d => d.Location.SourceTree!.FilePath, StringComparer.Ordinal)
    .ToList();

var totalFilesFixed = 0;
var totalAwaitsFixed = 0;

foreach (var fileGroup in diagnosticsByFile)
{
    var tree = fileGroup.First().Location.SourceTree!;
    var root = await tree.GetRootAsync().ConfigureAwait(false);
    var newRoot = root;

    foreach (var diagnostic in fileGroup
        .OrderByDescending(d => d.Location.SourceSpan.Start))
    {
        var found = root.FindNode(diagnostic.Location.SourceSpan);
        var awaitExpr = found.AncestorsAndSelf()
            .OfType<AwaitExpressionSyntax>()
            .FirstOrDefault();

        if (awaitExpr is null || SyntaxFixer.HasConfigureAwait(awaitExpr))
            continue;

        var nodeInNewRoot = newRoot.FindNode(awaitExpr.Span);
        newRoot = newRoot.ReplaceNode(nodeInNewRoot, SyntaxFixer.AddConfigureAwait((AwaitExpressionSyntax)nodeInNewRoot));
        totalAwaitsFixed++;
    }

    var newText = newRoot.GetText().ToString();
    if (string.Equals(newText, root.GetText().ToString(), StringComparison.Ordinal))
        continue;

    var parsed = CSharpSyntaxTree.ParseText(newText, path: fileGroup.Key);
    var parseErrors = parsed.GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToList();
    if (parseErrors.Count > 0)
    {
        await Console.Error.WriteLineAsync(
            $"Parse error after fix in {fileGroup.Key}: {parseErrors[0].Id} {parseErrors[0].GetMessage()} — skipping write")
            .ConfigureAwait(false);
        totalAwaitsFixed -= fileGroup.Count();
        continue;
    }

    await File.WriteAllTextAsync(fileGroup.Key, newText).ConfigureAwait(false);
    await Console.Error.WriteLineAsync(
        $"Fixed {fileGroup.Count().ToString(CultureInfo.InvariantCulture)} await(s) in {fileGroup.Key}").ConfigureAwait(false);
    totalFilesFixed++;
}

if (totalFilesFixed > 0)
{
    await Console.Error.WriteLineAsync(
        $"ConfigureAwaitFixer: {totalAwaitsFixed.ToString(CultureInfo.InvariantCulture)} await(s) fixed in {totalFilesFixed.ToString(CultureInfo.InvariantCulture)} file(s)")
        .ConfigureAwait(false);
}

stopwatch.Stop();
await Console.Error.WriteLineAsync(
    $"[ConfigureAwaitFixer] Completed in {stopwatch.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}s")
    .ConfigureAwait(false);

return 0;
