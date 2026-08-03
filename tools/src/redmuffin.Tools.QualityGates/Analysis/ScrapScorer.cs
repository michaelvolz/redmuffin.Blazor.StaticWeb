namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;

public static class ScrapScorer
{
    private const double ZeroAssertionPenalty = 5.0;
    private const double LowAssertionPenalty = 2.0;
    private const double BranchingPenaltyPerBranch = 1.0;
    private const double HighSetupPenaltyPerLevel = 1.0;
    private const int MaxWorstExamples = 5;

    public static TestMethodMetrics ScoreMethod(TestMethod method)
    {
        var body = method.BodySyntax;
        var lineCount = method.EndLine - method.StartLine + 1;

        var assertionCount = TestMethodMetricsCalculator.CountAssertions(body);
        var branchCount = TestMethodMetricsCalculator.CountBranches(body);
        var setupDepth = TestMethodMetricsCalculator.ComputeSetupDepth(body);
        var structuralComplexity = branchCount + 1;
        var complexityScore = TestMethodMetricsCalculator.ComputeComplexityScore(structuralComplexity);

        var smells = CollectSmells(assertionCount, branchCount, setupDepth);
        var scrapScore = ComputeScore(complexityScore, assertionCount, branchCount, setupDepth);
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

    private static List<SmellLabel> CollectSmells(int assertionCount, int branchCount, int setupDepth)
    {
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

        return smells;
    }

    private static double ComputeScore(
        double complexityScore, int assertionCount, int branchCount, int setupDepth)
    {
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

        return scrapScore;
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T13:13:02.0234277Z","moduleHash":"8fbfb885d4cf8308e363e08b4d7fdfc6241643cfdc0a56aaf75de0e11a26dc04","forms":[{"id":"ScoreMethod","line":12,"endLine":34,"hash":"e244937dedb92e5717a5d342af39f8e620e9aa771031641ec266734c581cf653"},{"id":"ScoreFile","line":36,"endLine":75,"hash":"36ef3d031fe8aa2b046c5e04f677fba0d32c1ed0faff8528da6cc2d00ec88c70"},{"id":"CollectSmells","line":77,"endLine":100,"hash":"c128b392cf100d01994b214163a45897fff19e9393448de7cca8b74ca5178ae2"},{"id":"ComputeScore","line":102,"endLine":122,"hash":"d69e751730f7ebd23d9eecf78d25aaa0dbdaf7e67f85866a5806b5d09d84f729"}]}
// clj-mutate-manifest-end
