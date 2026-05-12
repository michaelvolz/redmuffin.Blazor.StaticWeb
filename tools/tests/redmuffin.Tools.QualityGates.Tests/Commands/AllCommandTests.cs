namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Unit tests for extracted AllCommand helpers.
/// </summary>
public sealed class AllCommandTests
{
    [Test]
    public async Task WriteGateHeaderAsync_with_null_config_returns_false_and_writes_skip_message()
    {
        using var writer = new StringWriter();
        var result = await AllCommand.WriteGateHeaderAsync(
            writer, config: null, gateName: "Test Gate", missingFlag: "--test-flag")
            .ConfigureAwait(false);

        await Assert.That(result).IsFalse();
        await Assert.That(writer.ToString()).Contains("SKIPPED");
    }

    [Test]
    public async Task WriteGateHeaderAsync_with_config_returns_true_and_writes_header()
    {
        using var writer = new StringWriter();
        var result = await AllCommand.WriteGateHeaderAsync(
            writer, config: "/some/path", gateName: "Test Gate", missingFlag: "--test-flag")
            .ConfigureAwait(false);

        await Assert.That(result).IsTrue();
        await Assert.That(writer.ToString()).Contains("Test Gate");
        await Assert.That(writer.ToString()).DoesNotContain("SKIPPED");
    }

    [Test]
    public async Task BuildSummaryLine_with_failures_reports_fail()
    {
        var line = AllCommand.BuildSummaryLine(
            overallExit: 2, crapExit: 0, scrapExit: 0,
            archConfig: "/cfg.yml", archExit: 0,
            mutateSource: "/src.cs", mutateExit: 0,
            runDupes: false, dupesExit: 0);

        await Assert.That(line).Contains("Overall: FAIL");
    }

    [Test]
    public async Task BuildSummaryLine_with_na_gates_uses_na()
    {
        var line = AllCommand.BuildSummaryLine(
            overallExit: 0, crapExit: 0, scrapExit: 0,
            archConfig: null, archExit: 0,
            mutateSource: null, mutateExit: 0,
            runDupes: false, dupesExit: 0);

        await Assert.That(line).Contains("Architecture: N/A");
        await Assert.That(line).Contains("Mutation: N/A");
        await Assert.That(line).Contains("Duplicates: N/A");
    }

    [Test]
    public async Task CombineExitCodes_returns_two_when_any_fail()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 2, archExit: 0, mutateExit: 0, dupesExit: 0);
        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task CombineExitCodes_returns_one_when_any_error_no_fails()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 1, scrapExit: 0, archExit: 0, mutateExit: 0, dupesExit: 0);
        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task CombineExitCodes_returns_zero_when_all_pass()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 0, archExit: 0, mutateExit: 0, dupesExit: 0);
        await Assert.That(result).IsEqualTo(0);
    }
}
