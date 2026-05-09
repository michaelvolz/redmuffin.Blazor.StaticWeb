namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

public sealed class ScrapCommandTests
{
    [Test]
    public async Task should_return_exit_code_0_when_all_files_stable()
    {
        var report = CreateStableReport();
        var options = new ScrapOptions();
        var exitCode = ScrapHandler.Run([report], options);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    private static FileScrapReport CreateReport(
        string filePath = "/src/tests/TestFile.cs",
        int exampleCount = 4,
        double avgScrap = 3.0,
        double maxScrap = 5.0,
        double effectiveDupScore = 0.0,
        double zeroAssertionRatio = 0.0,
        double lowAssertionRatio = 0.25,
        double totalExtractionPressure = 0.0,
        IReadOnlyList<double>? clusterPressures = null,
        IReadOnlyList<DuplicationChannel>? harmfulDup = null,
        IReadOnlyList<DuplicationChannel>? caseMatrix = null,
        IReadOnlyList<DuplicationChannel>? subjectRep = null)
    {
        var dup = new DuplicationResults(
            HarmfulDuplication: harmfulDup ?? [],
            CaseMatrixRepetition: caseMatrix ?? [],
            SubjectRepetition: subjectRep ?? [],
            EffectiveDuplicationScore: effectiveDupScore);

        var pressure = new FilePressure(
            TotalExtractionPressure: totalExtractionPressure,
            ClusterPressures: clusterPressures ?? [],
            MatrixCredit: 0.0,
            NetPressure: totalExtractionPressure);

        var smells = new SmellCounts(
            BranchingCount: 0,
            LowAssertionCount: lowAssertionRatio > 0 ? (int)(exampleCount * lowAssertionRatio) : 0,
            ZeroAssertionCount: zeroAssertionRatio > 0 ? (int)(exampleCount * zeroAssertionRatio) : 0,
            ZeroAssertionRatio: zeroAssertionRatio,
            LowAssertionRatio: lowAssertionRatio);

        return new FileScrapReport(
            FilePath: filePath,
            ExampleCount: exampleCount,
            AvgScrap: avgScrap,
            MaxScrap: maxScrap,
            Metrics: [],
            DuplicationResults: dup,
            ExtractionPressure: pressure,
            SmellCounts: smells,
            WorstExamples: []);
    }

    private static FileScrapReport CreateStableReport() => CreateReport();

    [Test]
    public async Task should_return_exit_code_2_when_any_file_is_split()
    {
        var report = CreateSplitReport();
        var options = new ScrapOptions();
        var exitCode = ScrapHandler.Run([report], options);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_code_2_when_actionability_not_leave_alone()
    {
        var report = CreateReport(
            maxScrap: 13.0,
            zeroAssertionRatio: 0.25);
        var options = new ScrapOptions();
        var exitCode = ScrapHandler.Run([report], options);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_code_0_for_empty_results()
    {
        var options = new ScrapOptions();
        var exitCode = ScrapHandler.Run([], options);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_output_file_summary_with_path_and_mode()
    {
        var report = CreateStableReport();
        var options = new ScrapOptions();
        using var output = new StringWriter();

        var exitCode = ScrapHandler.Run([report], options, output);
        var text = output.ToString();

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(text).Contains("TestFile.cs");
        await Assert.That(text).Contains("Stable");
        await Assert.That(text).Contains("LeaveAlone");
    }

    private static FileScrapReport CreateSplitReport() =>
        CreateReport(
            exampleCount: 12,
            avgScrap: 10.0,
            maxScrap: 35.0);
}
