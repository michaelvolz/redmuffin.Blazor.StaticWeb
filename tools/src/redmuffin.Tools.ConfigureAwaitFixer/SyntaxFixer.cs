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

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:44:29.5419476Z","moduleHash":"09908328fa98b39c5baa6f00cad69ab0cf9f9994f803e53933b5bf3ec8d5f7bf","forms":[{"id":"IsSourceFile","line":15,"endLine":26,"hash":"831a6b1e3e6fdc7b6ab6df1e190574a52a15f86487c8c3d8b07dd64d125d661a"},{"id":"HasConfigureAwait","line":31,"endLine":39,"hash":"6fad4179120115960ca67c66223def407f5288260d85f622a48dbda0d1aa7876"},{"id":"AddConfigureAwait","line":44,"endLine":58,"hash":"6125249d4271a3789b238a7a70502f9ddec5f0079ec161ff76b44a4b7a76a2a1"}]}
// clj-mutate-manifest-end
