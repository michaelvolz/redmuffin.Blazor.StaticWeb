namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Unit tests for ArchHandler and ScrapCommand helpers.
/// </summary>
public sealed class GateHelperTests
{
    [Test]
    public async Task CheckBaselineMissing_with_null_returns_false()
    {
        var result = ScrapCommand.CheckBaselineMissing(null);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CheckBaselineMissing_with_nonexistent_file_returns_true()
    {
        var result = ScrapCommand.CheckBaselineMissing("/tmp/nonexistent-baseline.json");
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Run_with_valid_yaml_returns_success()
    {
        var toolsDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var srcRoot = Path.Combine(toolsDir, "src");
        var configPath = Path.Combine(toolsDir, "quality-gates", "architecture-rules.yml");

        if (!File.Exists(configPath)) return;

        var (exitCode, result) = ArchHandler.Run(configPath, srcRoot);
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Violations.Count).IsEqualTo(0);
        await Assert.That(result.ProjectsScanned).IsEqualTo(2);
    }

    [Test]
    public async Task ResolveFormat_json_true_returns_json()
    {
        var result = DupesCommand.ResolveFormat(json: true, formatOption: null);
        await Assert.That(result).IsEqualTo("json");
    }

    [Test]
    public async Task ResolveFormat_json_false_format_null_returns_text()
    {
        var result = DupesCommand.ResolveFormat(json: false, formatOption: null);
        await Assert.That(result).IsEqualTo("text");
    }
}
