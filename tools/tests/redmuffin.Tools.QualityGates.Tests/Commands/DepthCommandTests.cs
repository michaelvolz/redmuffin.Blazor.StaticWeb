namespace redmuffin.Tools.QualityGates.Tests.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Commands;

public sealed class DepthCommandTests
{
    private static string DepthFixturesDir => Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "depth-fixtures");

    [Test]
    [Category("Feature:Depth")]
    public async Task Create_returns_command_with_name_depth()
    {
        var command = DepthCommand.Create();

        await Assert.That(command.Name).IsEqualTo("depth");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Create_returns_command_with_project_option()
    {
        var command = DepthCommand.Create();

        await Assert.That(command.Options).IsNotEmpty();
        await Assert.That(command.Options[0].Name).IsEqualTo("--project");
        await Assert.That(command.Options[1].Name).IsEqualTo("--verbose");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Execute_returns_exit_code_one_for_nonexistent_directory()
    {
        var nonExistent = Path.Combine(Environment.CurrentDirectory, "does-not-exist-x");

        var exitCode = DepthCommand.Execute(nonExistent);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Execute_returns_exit_code_from_handler_for_actual_fixtures()
    {
        var exitCode = DepthCommand.Execute(DepthFixturesDir);

        // Fixture contains FAIL (composite=3 shallow + composite=4 combined) → exit 2
        await Assert.That(exitCode).IsEqualTo(2);
    }
}
