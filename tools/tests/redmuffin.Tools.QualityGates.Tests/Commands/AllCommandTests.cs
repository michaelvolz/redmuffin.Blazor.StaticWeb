namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

public sealed class AllCommandTests
{
    [Test]
    public async Task should_return_exit_0_when_all_gates_pass()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 0, archExit: 0);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_exit_2_when_crap_breaches()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 2, scrapExit: 0, archExit: 0);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_2_when_scrap_breaches()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 2, archExit: 0);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_2_when_arch_breaches()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 0, archExit: 2);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_1_when_arch_errors()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 0, archExit: 1);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task should_return_exit_2_when_multiple_fail()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 2, scrapExit: 0, archExit: 2);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_return_exit_2_when_scrap_fails_arch_clean()
    {
        var exitCode = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 2, archExit: 0);

        await Assert.That(exitCode).IsEqualTo(2);
    }
}
