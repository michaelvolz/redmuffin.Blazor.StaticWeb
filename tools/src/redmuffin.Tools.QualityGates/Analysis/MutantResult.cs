namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record MutantResult(
    int SiteIndex,
    MutationCategory Category,
    int Line,
    string Description,
    MutantResultType Result,
    long DurationMs);
