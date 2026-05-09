namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record TestMethodMetrics(
    TestMethod Method,
    int LineCount,
    double ComplexityScore,
    int AssertionCount,
    int SetupDepth,
    int BranchCount,
    double ScrapScore,
    IReadOnlyList<SmellLabel> SmellLabels);
