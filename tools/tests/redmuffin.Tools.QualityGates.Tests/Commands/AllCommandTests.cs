namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

public sealed class AllCommandTests
{
    [Test]
    public async Task should_return_exit_0_when_both_gates_pass()
    {
        var exitCode = AllCommand.CombineExitCodes(crapExit: 0, scrapExit: 0);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_exit_2_when_crap_breaches()
    {
        var exitCode = AllCommand.CombineExitCodes(crapExit: 2, scrapExit: 0);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_2_when_scrap_breaches()
    {
        var exitCode = AllCommand.CombineExitCodes(crapExit: 0, scrapExit: 2);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_1_when_crap_errors_and_scrap_passes()
    {
        var exitCode = AllCommand.CombineExitCodes(crapExit: 1, scrapExit: 0);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task should_return_exit_2_when_one_breaches_and_one_errors()
    {
        var exitCode = AllCommand.CombineExitCodes(crapExit: 1, scrapExit: 2);

        await Assert.That(exitCode).IsEqualTo(2);
    }
}
