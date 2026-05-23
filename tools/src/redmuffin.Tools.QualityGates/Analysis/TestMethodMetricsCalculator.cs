namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Single canonical home for test method metric computation.
/// Eliminates duplication between ScrapDuplication and ScrapScorer
/// and ensures consistent semantics across the SCRAP pipeline.
/// </summary>
public static class TestMethodMetricsCalculator
{
    public static int CountAssertions(SyntaxNode body) =>
        body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(i =>
            {
                if (i.Expression is MemberAccessExpressionSyntax ma)
                {
                    return ma.Expression.ToString().StartsWith("Assert", StringComparison.Ordinal)
                        && string.Equals(ma.Name.Identifier.Text, "That", StringComparison.Ordinal);
                }

                return false;
            });

    /// <summary>
    /// Counts branching constructs in a test body.
    /// </summary>
    public static int CountBranches(SyntaxNode body) =>
        body.DescendantNodes().Count(n =>
            n is IfStatementSyntax
            or SwitchStatementSyntax
            or WhileStatementSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax
            or ConditionalExpressionSyntax);

    public static int ComputeSetupDepth(SyntaxNode body)
    {
        if (body is not BlockSyntax block)
        {
            return 0;
        }

        var depth = 0;
        foreach (var stmt in block.Statements)
        {
            var hasAssert = stmt.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(i =>
                {
                    if (i.Expression is MemberAccessExpressionSyntax ma)
                    {
                        return ma.Expression.ToString().StartsWith("Assert", StringComparison.Ordinal);
                    }

                    return false;
                });

            if (hasAssert)
            {
                break;
            }

            depth++;
        }

        return depth;
    }
}
