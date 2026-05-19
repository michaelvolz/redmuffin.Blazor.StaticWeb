using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

if (string.Equals(
        Environment.GetEnvironmentVariable("CI"),
        "true",
        StringComparison.OrdinalIgnoreCase))
    return 0;

var stopwatch = Stopwatch.StartNew();
string? singleFile = null;
var targetDir = Environment.CurrentDirectory;

// Parse --file <path> flag
for (var i = 0; i < args.Length; i++)
{
    if (string.Equals(args[i], "--file", StringComparison.Ordinal))
    {
        if (i + 1 < args.Length)
        {
            singleFile = Path.GetFullPath(args[i + 1]);
            targetDir = Path.GetDirectoryName(singleFile)!;
            i++;
        }
        else
        {
            await Console.Error.WriteLineAsync("--file requires a path argument").ConfigureAwait(false);
            return 1;
        }
    }
    else
    {
        targetDir = args[i];
    }
}

// Walk up from targetDir to find .csproj
var csprojFiles = new List<string>();
var searchDir = targetDir;
while (searchDir is not null)
{
    csprojFiles.AddRange(Directory.EnumerateFiles(searchDir, "*.csproj", SearchOption.TopDirectoryOnly));
    if (csprojFiles.Count > 0)
        break;
    searchDir = Path.GetDirectoryName(searchDir);
}

if (csprojFiles.Count == 0)
{
    await Console.Error.WriteLineAsync($"No .csproj found above {targetDir}").ConfigureAwait(false);
    return 1;
}

var projectPath = csprojFiles[0];

// Load the official CA2007 analyzer
// Load the official CA2007 analyzer from both NetAnalyzers assemblies
var analyzerAssemblies = new[]
{
    Path.Combine(AppContext.BaseDirectory, "Microsoft.CodeAnalysis.NetAnalyzers.dll"),
    Path.Combine(AppContext.BaseDirectory, "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll"),
};

var analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
foreach (var dll in analyzerAssemblies)
{
    if (!File.Exists(dll))
    {
        await Console.Error.WriteLineAsync($"Analyzer DLL not found: {dll}").ConfigureAwait(false);
        return 1;
    }

    var assembly = Assembly.LoadFrom(dll);
    var loaded = assembly.GetTypes()
        .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
        .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!);
    analyzers = analyzers.AddRange(loaded);
}

if (analyzers.IsEmpty)
{
    await Console.Error.WriteLineAsync("No DiagnosticAnalyzer types found in analyzer DLL.").ConfigureAwait(false);
    return 1;
}

// Load the project and get CA2007 diagnostics
using var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath).ConfigureAwait(false);
var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
if (compilation is null)
{
    await Console.Error.WriteLineAsync($"Failed to get compilation for {projectPath}").ConfigureAwait(false);
    return 1;
}

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
        && IsSourceFile(d.Location.SourceTree.FilePath)
        && (singleFile is null
            || string.Equals(d.Location.SourceTree.FilePath, singleFile, StringComparison.Ordinal)))
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

        if (awaitExpr is null || HasConfigureAwait(awaitExpr))
            continue;

        // Find the same node in newRoot (which may have been modified by prior fixes)
        var nodeInNewRoot = newRoot.FindNode(awaitExpr.Span);
        newRoot = newRoot.ReplaceNode(nodeInNewRoot, AddConfigureAwait((AwaitExpressionSyntax)nodeInNewRoot));
        totalAwaitsFixed++;
    }

    var newText = newRoot.GetText().ToString();
    if (string.Equals(newText, root.GetText().ToString(), StringComparison.Ordinal))
        continue;

    // Verify the fix parses
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
var elapsed = stopwatch.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
await Console.Error.WriteLineAsync(
    $"[ConfigureAwaitFixer] Completed in {elapsed}s")
    .ConfigureAwait(false);

return 0;

static bool IsSourceFile(string path)
{
    var fileName = Path.GetFileName(path);
    if (fileName.EndsWith(".Designer.cs", StringComparison.Ordinal)
        || fileName.EndsWith(".g.cs", StringComparison.Ordinal)
        || fileName.EndsWith("_AssemblyInfo.cs", StringComparison.Ordinal))
        return false;

    var dirName = Path.GetDirectoryName(path) ?? string.Empty;
    return !dirName.Contains("obj", StringComparison.Ordinal)
        && !dirName.Contains("bin", StringComparison.Ordinal);
}

static bool HasConfigureAwait(AwaitExpressionSyntax expr)
{
    return expr.Expression is InvocationExpressionSyntax invocation
        && invocation.Expression is MemberAccessExpressionSyntax member
        && string.Equals(
            member.Name.Identifier.Text,
            "ConfigureAwait",
            StringComparison.Ordinal);
}

static AwaitExpressionSyntax AddConfigureAwait(AwaitExpressionSyntax awaitExpr)
{
    var newExpr = SyntaxFactory.InvocationExpression(
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            awaitExpr.Expression,
            SyntaxFactory.IdentifierName("ConfigureAwait")))
        .WithArgumentList(SyntaxFactory.ArgumentList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.FalseLiteralExpression)))));

    return awaitExpr.WithExpression(newExpr);
}
