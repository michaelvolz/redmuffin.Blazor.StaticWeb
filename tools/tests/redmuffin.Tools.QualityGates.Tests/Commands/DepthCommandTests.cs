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

    [Test]
    [Category("Feature:Depth")]
    public async Task Execute_verbose_mode_produces_per_file_summary()
    {
        using var stringWriter = new StringWriter();
        var exitCode = DepthCommand.Execute(DepthFixturesDir, verbose: true, output: stringWriter);

        await Assert.That(exitCode).IsEqualTo(2);

        var output = stringWriter.ToString();
        await Assert.That(output).Contains("Analyzing structural depth in:");
        await Assert.That(output).Contains("method(s) with issues");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Execute_returns_exit_code_zero_for_public_only_methods()
    {
        var emptyProject = Path.Combine(Path.GetTempPath(), $"depth-empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(emptyProject);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(emptyProject, "Empty.cs"),
                "public class X { public void M() { } }").ConfigureAwait(false);

            using var stringWriter = new StringWriter();
            var exitCode = DepthCommand.Execute(emptyProject, output: stringWriter);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(stringWriter.ToString()).Contains("No methods with structural depth issues found.");
        }
        finally
        {
            Directory.Delete(emptyProject, recursive: true);
        }
    }
}
