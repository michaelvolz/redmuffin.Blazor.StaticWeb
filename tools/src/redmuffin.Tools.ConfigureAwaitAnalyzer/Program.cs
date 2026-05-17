using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var dir = args.Length > 0 ? args[0] : Environment.CurrentDirectory;

if (!Directory.Exists(dir))
{
    await Console.Error.WriteLineAsync($"Directory not found: {dir}").ConfigureAwait(false);
    return 1;
}

var csFiles = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
    .Where(IsSourceFile)
    .ToList();

if (csFiles.Count == 0)
    return 0;

// Build a compilation with reference assemblies for semantic analysis
var references = new List<MetadataReference>
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
};

var syntaxTrees = csFiles
    .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f))
    .ToList();

var compilation = CSharpCompilation.Create(
    "ConfigureAwaitFix",
    syntaxTrees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

var asyncAwaitableTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Task",
    "Task`1",
    "ValueTask",
    "ValueTask`1",
};

var totalFilesFixed = 0;
var totalAwaitsFixed = 0;

for (var i = 0; i < csFiles.Count; i++)
{
    var file = csFiles[i];
    var tree = syntaxTrees[i];
    var model = compilation.GetSemanticModel(tree);
    var root = tree.GetCompilationUnitRoot();

    var awaits = root.DescendantNodes()
        .OfType<AwaitExpressionSyntax>()
        .Where(e => !HasConfigureAwait(e) && IsAwaitableTask(model, e, asyncAwaitableTypes))
        .ToList();

    if (awaits.Count == 0)
        continue;

    var newRoot = root.ReplaceNodes(
        awaits,
        (original, _) => AddConfigureAwait(original));

    var newText = newRoot.GetText().ToString();
    if (string.Equals(newText, root.GetText().ToString(), StringComparison.Ordinal))
        continue;

    await File.WriteAllTextAsync(file, newText).ConfigureAwait(false);
    await Console.Error.WriteLineAsync(
        $"Fixed {awaits.Count.ToString(CultureInfo.InvariantCulture)} await(s) in {file}").ConfigureAwait(false);
    totalFilesFixed++;
    totalAwaitsFixed += awaits.Count;
}

if (totalFilesFixed > 0)
{
    await Console.Error.WriteLineAsync(
        $"ConfigureAwaitFixer: {totalAwaitsFixed.ToString(CultureInfo.InvariantCulture)} await(s) fixed in {totalFilesFixed.ToString(CultureInfo.InvariantCulture)} file(s)")
        .ConfigureAwait(false);
}

return 0;

static bool IsSourceFile(string path)
{
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

static bool IsAwaitableTask(
    SemanticModel model,
    AwaitExpressionSyntax awaitExpr,
    HashSet<string> asyncAwaitableTypes)
{
    var typeInfo = model.GetTypeInfo(awaitExpr.Expression);
    var type = typeInfo.Type;

    if (type is null)
        return false;

    // Must be in System.Threading.Tasks namespace
    if (!string.Equals(type.ContainingNamespace?.ToDisplayString(), "System.Threading.Tasks", StringComparison.Ordinal))
        return false;

    return asyncAwaitableTypes.Contains(type.MetadataName);
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
