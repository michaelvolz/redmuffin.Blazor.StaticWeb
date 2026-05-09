namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class ScrapRecommenderTests
{
    [Test]
    public async Task should_classify_stable_for_well_structured_file()
    {
        var report = CreateReport(
            exampleCount: 5,
            maxScrap: 8.0,
            avgScrap: 5.0,
            effectiveDuplication: 2.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.2);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Stable);
        await Assert.That(recommendation.AiActionability).IsEqualTo(AiActionability.LeaveAlone);
    }

    [Test]
    public async Task should_not_be_stable_with_zero_assertion()
    {
        var report = CreateReport(
            exampleCount: 5,
            maxScrap: 8.0,
            avgScrap: 5.0,
            effectiveDuplication: 2.0,
            zeroAssertRatio: 0.2,
            lowAssertRatio: 0.2);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsNotEqualTo(StabilityMode.Stable);
    }

    [Test]
    public async Task should_classify_split_for_large_problematic_file()
    {
        var report = CreateReport(
            exampleCount: 15,
            maxScrap: 14.0,
            avgScrap: 11.0,
            effectiveDuplication: 5.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.4,
            subjectRepetition: 13,
            highPressureBlocks: 2);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Split);
        await Assert.That(recommendation.AiActionability).IsEqualTo(AiActionability.ManualSplit);
    }

    [Test]
    public async Task should_classify_local_for_moderate_file()
    {
        var report = CreateReport(
            exampleCount: 8,
            maxScrap: 14.0,
            avgScrap: 7.0,
            effectiveDuplication: 4.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.3);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Local);
    }

    [Test]
    public async Task should_recommend_auto_refactor_for_local_with_smells()
    {
        var report = CreateReport(
            exampleCount: 8,
            maxScrap: 14.0,
            avgScrap: 7.0,
            effectiveDuplication: 4.0,
            zeroAssertRatio: 0.1,
            lowAssertRatio: 0.3,
            harmfulDupCount: 1);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Local);
        await Assert.That(recommendation.AiActionability).IsEqualTo(AiActionability.AutoRefactor);
    }

    [Test]
    public async Task should_classify_small_file_stable_with_tighter_bounds()
    {
        var report = CreateReport(
            exampleCount: 2,
            maxScrap: 9.0,
            avgScrap: 5.0,
            effectiveDuplication: 1.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.0);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Stable);
    }

    [Test]
    public async Task should_not_be_stable_small_file_with_max_scrap_over_10()
    {
        var report = CreateReport(
            exampleCount: 2,
            maxScrap: 11.0,
            avgScrap: 5.0,
            effectiveDuplication: 1.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.0);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsNotEqualTo(StabilityMode.Stable);
    }

    [Test]
    public async Task should_recommend_auto_table_drive_for_case_matrix()
    {
        var report = CreateReport(
            exampleCount: 5,
            maxScrap: 10.0,
            avgScrap: 6.0,
            effectiveDuplication: 2.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.0,
            caseMatrixCount: 2);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.AiActionability).IsEqualTo(AiActionability.AutoTableDrive);
    }

    [Test]
    public async Task should_fall_through_to_review_first_when_no_conditions_match()
    {
        var report = CreateReport(
            exampleCount: 6,
            maxScrap: 20.0,
            avgScrap: 8.0,
            effectiveDuplication: 5.0,
            zeroAssertRatio: 0.0,
            lowAssertRatio: 0.0);

        var recommendation = ScrapRecommender.Decide(report);

        await Assert.That(recommendation.Mode).IsEqualTo(StabilityMode.Local);
        await Assert.That(recommendation.AiActionability).IsEqualTo(AiActionability.ReviewFirst);
    }

    /// <summary>Creates a FileScrapReport with specified values for testing.</summary>
    private static FileScrapReport CreateReport(
        int exampleCount,
        double maxScrap,
        double avgScrap,
        double effectiveDuplication,
        double zeroAssertRatio,
        double lowAssertRatio,
        int harmfulDupCount = 0,
        int subjectRepetition = 0,
        int highPressureBlocks = 0,
        int caseMatrixCount = 0)
    {
        var metrics = Enumerable.Range(0, exampleCount)
            .Select(i => new TestMethodMetrics(
                new TestMethod($"Test{i}", "Test.cs", 1, 1, null!, "MyTests"),
                5, 1.0, 2, 0, 0, 1.0,
                Array.Empty<SmellLabel>()))
            .ToList();

        var harmful = harmfulDupCount > 0
            ? new[] { new DuplicationChannel(1, Array.Empty<TestMethod>(), 3, 3, harmfulDupCount, ChannelType.Harmful) }
            : Array.Empty<DuplicationChannel>();

        var caseMatrix = caseMatrixCount > 0
            ? Enumerable.Range(0, caseMatrixCount)
                .Select(i => new DuplicationChannel(i + 100, Array.Empty<TestMethod>(), 0, 0, 3, ChannelType.CaseMatrix))
                .ToArray()
            : Array.Empty<DuplicationChannel>();

        var subject = subjectRepetition > 0
            ? new[] { new DuplicationChannel(200, Array.Empty<TestMethod>(), 0, 0, subjectRepetition, ChannelType.Subject) }
            : Array.Empty<DuplicationChannel>();

        var dupeResults = new DuplicationResults(harmful, caseMatrix, subject, effectiveDuplication);

        var pressures = Enumerable.Range(0, highPressureBlocks)
            .Select(_ => 20.0)
            .Concat(new[] { 5.0 })
            .ToArray();
        var pressure = new FilePressure(pressures.Sum(), pressures, 0.0, pressures.Sum());

        var smells = new SmellCounts(
            0,
            (int)(lowAssertRatio * exampleCount),
            (int)(zeroAssertRatio * exampleCount),
            zeroAssertRatio,
            lowAssertRatio);

        return new FileScrapReport(
            "Test.cs", exampleCount, avgScrap, maxScrap,
            metrics, dupeResults, pressure, smells, metrics.Take(3).ToList());
    }
}
