namespace redmuffin.Tools.QualityGates.Tests.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Commands;

public sealed class CommandFactoryTests
{
    [Test]
    public async Task ArchCommand_Create_returns_command_with_required_options()
    {
        var cmd = ArchCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Options.Count).IsGreaterThan(0);
        await Assert.That(cmd.Name).IsEqualTo("architecture");
    }

    [Test]
    public async Task MutateCommand_Create_returns_command_with_options()
    {
        var cmd = MutateCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Options.Count).IsGreaterThan(0);
        await Assert.That(cmd.Name).IsEqualTo("mutation");
    }

    [Test]
    public async Task CrapCommand_Create_returns_command()
    {
        var cmd = CrapCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Name).IsEqualTo("crap");
    }

    [Test]
    public async Task ScrapCommand_Create_returns_command()
    {
        var cmd = ScrapCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Name).IsEqualTo("scrap");
    }

    [Test]
    public async Task DupesCommand_Create_returns_command()
    {
        var cmd = DupesCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Name).IsEqualTo("duplicates");
    }

    [Test]
    public async Task AllCommand_Create_returns_command()
    {
        var cmd = AllCommand.Create();
        await Assert.That(cmd).IsNotNull();
        await Assert.That(cmd.Name).IsEqualTo("all");
    }
}
