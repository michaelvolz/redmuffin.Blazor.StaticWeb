using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Pure syntax manipulation functions for the ConfigureAwaitFixer.
///     No I/O, no Roslyn workspace — operates on syntax nodes only.
/// </summary>
public static class SyntaxFixer
{
    /// <summary>
    ///     Returns true if the file path is a user-editable source file
    ///     (excludes generated files and obj/bin directories).
    /// </summary>
    public static bool IsSourceFile(string path)
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

    /// <summary>
    ///     Returns true if the await expression already has a <c>.ConfigureAwait(...)</c> call.
    /// </summary>
    public static bool HasConfigureAwait(AwaitExpressionSyntax expr)
    {
        return expr.Expression is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax member
            && string.Equals(
                member.Name.Identifier.Text,
                "ConfigureAwait",
                StringComparison.Ordinal);
    }

    /// <summary>
    ///     Wraps the awaited expression in a <c>.ConfigureAwait(false)</c> call.
    /// </summary>
    public static AwaitExpressionSyntax AddConfigureAwait(AwaitExpressionSyntax awaitExpr)
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
}
