namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record FileScrapReport(
    string FilePath,
    int ExampleCount,
    double AvgScrap,
    double MaxScrap,
    IReadOnlyList<TestMethodMetrics> Metrics,
    DuplicationResults DuplicationResults,
    FilePressure ExtractionPressure,
    SmellCounts SmellCounts,
    IReadOnlyList<TestMethodMetrics> WorstExamples);
