using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Shared CA2007 fix logic used by both the one-shot pipeline and the
///     daemon. Pure: no I/O, no workspace — operates on text and diagnostics.
/// </summary>
public static class FixRunner
{
    /// <summary>
    ///     Applies CA2007 fixes to <paramref name="originalText"/> at the
    ///     locations reported by <paramref name="diagnostics"/> (which must all
    ///     belong to the same file). Returns null text when nothing was fixed.
    /// </summary>
    public static FixOutcome ApplyFixes(IEnumerable<Diagnostic> diagnostics, string filePath, string originalText)
    {
        var root = CSharpSyntaxTree.ParseText(originalText, path: filePath).GetRoot();
        var newRoot = root;
        var fixedAwaits = 0;

        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            var found = root.FindNode(diagnostic.Location.SourceSpan);
            var awaitExpr = found.AncestorsAndSelf()
                .OfType<AwaitExpressionSyntax>()
                .FirstOrDefault();

            if (awaitExpr is null || SyntaxFixer.HasConfigureAwait(awaitExpr))
                continue;

            var nodeInNewRoot = newRoot.FindNode(awaitExpr.Span);
            newRoot = newRoot.ReplaceNode(nodeInNewRoot, SyntaxFixer.AddConfigureAwait((AwaitExpressionSyntax)nodeInNewRoot));
            fixedAwaits++;
        }

        if (fixedAwaits == 0)
            return new FixOutcome(null, 0, null);

        var newText = newRoot.GetText().ToString();
        var parseError = CSharpSyntaxTree.ParseText(newText, path: filePath)
            .GetDiagnostics()
            .FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        if (parseError is not null)
            return new FixOutcome(null, fixedAwaits, $"{parseError.Id} {parseError.GetMessage()}");

        return new FixOutcome(newText, fixedAwaits, null);
    }

    /// <summary>
    ///     The result of applying fixes to one file: the fixed text, the
    ///     number of awaits fixed, or a parse error message when the rewrite
    ///     produced invalid syntax (in which case nothing should be written).
    /// </summary>
    public sealed record FixOutcome(string? NewText, int FixedAwaits, string? ParseError);
}
