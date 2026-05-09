namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record SmellCounts(
    int BranchingCount,
    int LowAssertionCount,
    int ZeroAssertionCount,
    double ZeroAssertionRatio,
    double LowAssertionRatio);
