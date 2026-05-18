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

var totalFilesFixed = 0;
var totalAwaitsFixed = 0;

foreach (var file in csFiles)
{
    var text = await File.ReadAllTextAsync(file).ConfigureAwait(false);
    var tree = CSharpSyntaxTree.ParseText(text, path: file);
    var root = tree.GetCompilationUnitRoot();

    var awaits = root.DescendantNodes()
        .OfType<AwaitExpressionSyntax>()
        .Where(e => !HasConfigureAwait(e) && !IsTestAssertion(e))
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

/// <summary>
///     Skip awaits on test assertion chains (TUnit, xUnit, NUnit).
///     These return assertion-builders that are not real tasks — adding
///     <c>.ConfigureAwait(false)</c> would produce a type error.
/// </summary>
static bool IsTestAssertion(AwaitExpressionSyntax expr)
{
    return StartsWithAssertChain(expr.Expression);
}

static bool StartsWithAssertChain(ExpressionSyntax expression)
{
    // Walk through member access and invocation chains to find the root identifier.
    // Assert.That(...).IsEmpty() → invocation(IsEmpty) → memberAccess(That) → identifier(Assert)
    while (true)
    {
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            expression = memberAccess.Expression;
        }
        else if (expression is InvocationExpressionSyntax invocation)
        {
            expression = invocation.Expression;
        }
        else
        {
            break;
        }
    }

    // The root of the chain should be an identifier like "Assert"
    return expression is IdentifierNameSyntax identifier
        && string.Equals(
            identifier.Identifier.Text,
            "Assert",
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
