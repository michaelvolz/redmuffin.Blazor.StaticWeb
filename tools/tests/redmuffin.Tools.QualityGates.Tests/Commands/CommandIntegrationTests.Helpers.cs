namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;

public partial class CommandIntegrationTests
{
    private static TestMethodMetrics DummyMetric(string methodName = "Test1", double scrapScore = 3.2) =>
        new(
            new TestMethod(methodName, $"void {methodName}() => 1;", 1, 1, null!, "MyTests"),
            LineCount: 1,
            ComplexityScore: 1,
            AssertionCount: 1,
            SetupDepth: 0,
            BranchCount: 1,
            ScrapScore: scrapScore,
            SmellLabels: []);

    private static FileScrapReport MakeScrapReport(
        string path,
        double avgScrap = 2.1,
        double maxScrap = 3.2,
        int exampleCount = 1,
        IReadOnlyList<TestMethodMetrics>? metrics = null)
    {
        metrics ??= [DummyMetric()];
        return new FileScrapReport(
            path,
            ExampleCount: metrics.Count,
            AvgScrap: avgScrap,
            MaxScrap: maxScrap,
            Metrics: metrics,
            DuplicationResults: new DuplicationResults([], [], [], 0),
            ExtractionPressure: new FilePressure(0, [], 0, 0),
            SmellCounts: new SmellCounts(0, 0, 0, 0, 0),
            WorstExamples: metrics);
    }
}
