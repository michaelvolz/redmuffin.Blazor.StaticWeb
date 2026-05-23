namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

public sealed class ScrapCommandTests
{
    [Test]
    public async Task IsDirectoryMissing_should_return_false_when_directory_exists()
    {
        var path = AppContext.BaseDirectory;

        var result = ScrapCommand.IsDirectoryMissing(path);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsDirectoryMissing_should_return_true_when_directory_missing()
    {
        var result = ScrapCommand.IsDirectoryMissing("/nonexistent/dir/xyzzy");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ValidateScrapInputs_should_return_false_when_both_valid()
    {
        var result = ScrapCommand.ValidateScrapInputs(
            AppContext.BaseDirectory, comparePath: null);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ValidateScrapInputs_should_return_true_when_directory_missing()
    {
        var result = ScrapCommand.ValidateScrapInputs(
            "/nonexistent", comparePath: null);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ValidateScrapInputs_should_return_true_when_baseline_missing()
    {
        var result = ScrapCommand.ValidateScrapInputs(
            AppContext.BaseDirectory, comparePath: "/nonexistent/baseline.json");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CheckBaselineMissing_should_return_false_when_null()
    {
        var result = ScrapCommand.CheckBaselineMissing(null);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CheckBaselineMissing_should_return_false_when_file_exists()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            var result = ScrapCommand.CheckBaselineMissing(tempPath);
            await Assert.That(result).IsFalse();
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Test]
    public async Task CheckBaselineMissing_should_return_true_when_file_missing()
    {
        var result = ScrapCommand.CheckBaselineMissing("/nonexistent/baseline.json");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Execute_should_return_0_when_no_test_methods()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var exitCode = ScrapCommand.Execute(
                tempDir, verbose: false, json: false,
                changedOnly: false, writeBaseline: false, comparePath: null);

            await Assert.That(exitCode).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Execute_should_return_1_when_directory_missing()
    {
        var exitCode = ScrapCommand.Execute(
            "/nonexistent/dir", verbose: false, json: false,
            changedOnly: false, writeBaseline: false, comparePath: null);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Execute_should_return_1_when_baseline_missing()
    {
        var exitCode = ScrapCommand.Execute(
            AppContext.BaseDirectory, verbose: false, json: false,
            changedOnly: false, writeBaseline: false,
            comparePath: "/nonexistent/baseline.json");

        await Assert.That(exitCode).IsEqualTo(1);
    }
}
