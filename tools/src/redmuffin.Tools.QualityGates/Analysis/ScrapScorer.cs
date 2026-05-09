namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class ScrapScorer
{
    private const double ComplexityCap = 25.0;
    private const double ComplexityRiseRate = 0.18;
    private const double ComplexityFloor = 1.0;
    private const double ZeroAssertionPenalty = 5.0;
    private const double LowAssertionPenalty = 2.0;
    private const double BranchingPenaltyPerBranch = 1.0;
    private const double HighSetupPenaltyPerLevel = 1.0;
    private const int MaxWorstExamples = 5;

    /// <summary>Computes per-example SCRAP metrics for a single test method.</summary>
    /// <returns></returns>
    public static TestMethodMetrics ScoreMethod(TestMethod method)
    {
        var body = method.BodySyntax;
        var lineCount = method.EndLine - method.StartLine + 1;

        var assertionCount = CountAssertions(body);
        var branchCount = CountBranches(body);
        var setupDepth = ComputeSetupDepth(body);
        var structuralComplexity = branchCount + 1;
        var complexityScore = Math.Min(ComplexityCap, ComplexityFloor + (ComplexityRiseRate * structuralComplexity));

        var smells = new List<SmellLabel>();
        if (assertionCount == 0)
        {
            smells.Add(SmellLabel.ZeroAssertion);
        }
        else if (assertionCount == 1)
        {
            smells.Add(SmellLabel.LowAssertion);
        }

        if (branchCount > 0)
        {
            smells.Add(SmellLabel.Branching);
        }

        if (setupDepth > 2)
        {
            smells.Add(SmellLabel.HighSetupDepth);
        }

        var scrapScore = complexityScore;
        if (assertionCount == 0)
        {
            scrapScore += ZeroAssertionPenalty;
        }
        else if (assertionCount == 1)
        {
            scrapScore += LowAssertionPenalty;
        }

        scrapScore += branchCount * BranchingPenaltyPerBranch;
        if (setupDepth > 2)
        {
            scrapScore += (setupDepth - 2) * HighSetupPenaltyPerLevel;
        }

        return new TestMethodMetrics(
            Method: method,
            LineCount: lineCount,
            ComplexityScore: complexityScore,
            AssertionCount: assertionCount,
            SetupDepth: setupDepth,
            BranchCount: branchCount,
            ScrapScore: scrapScore,
            SmellLabels: smells);
    }

    /// <summary>Aggregates per-method metrics into a file-level report.</summary>
    /// <returns></returns>
    public static FileScrapReport ScoreFile(
        IReadOnlyList<TestMethod> methods,
        DuplicationResults duplicationResults,
        FilePressure extractionPressure)
    {
        var metrics = methods.Select(ScoreMethod).ToList();
        var exampleCount = metrics.Count;

        var avgScrap = exampleCount > 0 ? metrics.Average(m => m.ScrapScore) : 0.0;
        var maxScrap = exampleCount > 0 ? metrics.Max(m => m.ScrapScore) : 0.0;

        var branchingCount = metrics.Count(m => m.SmellLabels.Contains(SmellLabel.Branching));
        var zeroAssertCount = metrics.Count(m => m.SmellLabels.Contains(SmellLabel.ZeroAssertion));
        var lowAssertCount = metrics.Count(m => m.SmellLabels.Contains(SmellLabel.LowAssertion));

        var smellCounts = new SmellCounts(
            BranchingCount: branchingCount,
            LowAssertionCount: lowAssertCount,
            ZeroAssertionCount: zeroAssertCount,
            ZeroAssertionRatio: exampleCount > 0 ? (double)zeroAssertCount / exampleCount : 0.0,
            LowAssertionRatio: exampleCount > 0 ? (double)lowAssertCount / exampleCount : 0.0);

        var worstExamples = metrics
            .OrderByDescending(m => m.ScrapScore)
            .Take(MaxWorstExamples)
            .ToList();

        var filePath = methods.Count > 0 ? methods[0].FilePath : string.Empty;

        return new FileScrapReport(
            FilePath: filePath,
            ExampleCount: exampleCount,
            AvgScrap: avgScrap,
            MaxScrap: maxScrap,
            Metrics: metrics,
            DuplicationResults: duplicationResults,
            ExtractionPressure: extractionPressure,
            SmellCounts: smellCounts,
            WorstExamples: worstExamples);
    }

    private static int CountAssertions(SyntaxNode body)
    {
        return body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(i =>
            {
                if (i.Expression is MemberAccessExpressionSyntax ma)
                {
                    var exprStr = ma.Expression.ToString();
                    return exprStr.StartsWith("Assert", StringComparison.Ordinal)
                        && string.Equals(ma.Name.Identifier.Text, "That", StringComparison.Ordinal);
                }

                return false;
            });
    }

    private static int CountBranches(SyntaxNode body)
    {
        return body.DescendantNodes().Count(n =>
            n is IfStatementSyntax
            or SwitchStatementSyntax
            or WhileStatementSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax
            or ConditionalExpressionSyntax);
    }

    private static int ComputeSetupDepth(SyntaxNode body)
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
