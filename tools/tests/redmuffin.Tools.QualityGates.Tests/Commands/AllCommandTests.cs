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
            runDupes: false, dupesExit: 0,
            runDepth: true, depthExit: 0);

        await Assert.That(line).Contains("Overall: FAIL");
    }

    [Test]
    public async Task BuildSummaryLine_with_na_gates_uses_na()
    {
        var line = AllCommand.BuildSummaryLine(
            overallExit: 0, crapExit: 0, scrapExit: 0,
            archConfig: null, archExit: 0,
            mutateSource: null, mutateExit: 0,
            runDupes: false, dupesExit: 0,
            runDepth: false, depthExit: 0);

        await Assert.That(line).Contains("Architecture: N/A");
        await Assert.That(line).Contains("Depth: N/A");
        await Assert.That(line).Contains("Mutation: N/A");
        await Assert.That(line).Contains("Duplicates: N/A");
    }

    [Test]
    public async Task CombineExitCodes_returns_two_when_any_fail()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 2, archExit: 0, mutateExit: 0, dupesExit: 0, depthExit: 0);
        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task CombineExitCodes_returns_one_when_any_error_no_fails()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 1, scrapExit: 0, archExit: 0, mutateExit: 0, dupesExit: 0, depthExit: 0);
        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task CombineExitCodes_returns_zero_when_all_pass()
    {
        var result = AllCommand.CombineExitCodes(
            crapExit: 0, scrapExit: 0, archExit: 0, mutateExit: 0, dupesExit: 0, depthExit: 0);
        await Assert.That(result).IsEqualTo(0);
    }

    // ── ResolveArchConfig ──

    [Test]
    public async Task should_return_provided_config_immediately()
    {
        var result = AllCommand.ResolveArchConfig("/custom/config.yml", "/any/project");

        await Assert.That(result).IsEqualTo("/custom/config.yml");
    }

    [Test]
    public async Task should_find_config_in_project_directory()
    {
        using var temp = new TempDirectory();
        temp.CreateDir("quality-gates");
        temp.CreateFile("quality-gates/architecture-rules.yml");

        var result = AllCommand.ResolveArchConfig(null, temp.Root);

        await Assert.That(result).IsEqualTo(
            Path.Combine(temp.Root, "quality-gates", "architecture-rules.yml"));
    }

    [Test]
    public async Task should_walk_up_to_find_config_in_parent()
    {
        using var temp = new TempDirectory();
        temp.CreateDir("quality-gates");
        temp.CreateFile("quality-gates/architecture-rules.yml");
        var projectDir = temp.CreateDir("src", "MyProject");

        var result = AllCommand.ResolveArchConfig(null, projectDir);

        await Assert.That(result).IsEqualTo(
            Path.Combine(temp.Root, "quality-gates", "architecture-rules.yml"));
    }

    [Test]
    public async Task should_return_null_when_no_config_found()
    {
        using var temp = new TempDirectory();
        var projectDir = temp.CreateDir("src", "MyProject");

        var result = AllCommand.ResolveArchConfig(null, projectDir);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task should_find_config_two_levels_up()
    {
        using var temp = new TempDirectory();
        temp.CreateDir("quality-gates");
        temp.CreateFile("quality-gates/architecture-rules.yml");
        var projectDir = temp.CreateDir("src", "SubDir", "MyProject");

        var result = AllCommand.ResolveArchConfig(null, projectDir);

        await Assert.That(result).IsEqualTo(
            Path.Combine(temp.Root, "quality-gates", "architecture-rules.yml"));
    }

    // ── Helpers ──

    // ── ProjectDir ──

    [Test]
    public async Task ProjectDir_should_strip_csproj_extension()
    {
        var result = AllCommand.ProjectDir("/some/path/project.csproj");

        await Assert.That(result).IsEqualTo("/some/path");
    }

    [Test]
    public async Task ProjectDir_should_return_directory_unchanged()
    {
        var result = AllCommand.ProjectDir("/some/path");

        await Assert.That(result).IsEqualTo("/some/path");
    }

    // ── DiscoverFromSourceOrSolution ──

    [Test]
    public async Task DiscoverFromSourceOrSolution_should_throw_when_solution_not_found()
    {
        var nonExistent = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
        {
            AllCommand.DiscoverFromSourceOrSolution(nonExistent);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task DiscoverFromSourceOrSolution_should_discover_from_null()
    {
        // Walks up from cwd to find .slnx — expect discovery or InvalidOperationException
        var result = AllCommand.DiscoverFromSourceOrSolution(null);

        await Assert.That(result.SourceProjects.Count).IsGreaterThan(0);
    }

    // ── ResolveTestProjectPaths ──

    [Test]
    public async Task ResolveTestProjectPaths_should_throw_when_testProject_not_found()
    {
        var nonExistent = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
        {
            AllCommand.ResolveTestProjectPaths(nonExistent, null);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ResolveTestProjectPaths_should_return_testProject_when_provided()
    {
        using var temp = new TempDirectory();

        var result = AllCommand.ResolveTestProjectPaths(
            new DirectoryInfo(temp.Root), null);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(temp.Root);
    }

    [Test]
    public async Task ResolveTestProjectPaths_should_discover_when_testProject_is_null()
    {
        var result = AllCommand.ResolveTestProjectPaths(null, null);

        await Assert.That(result.Count).IsGreaterThan(0);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; }

        public TempDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Root);
        }

        public string CreateDir(params string[] segments)
        {
            var path = Path.Combine(segments.Prepend(Root).ToArray());
            Directory.CreateDirectory(path);
            return path;
        }

        public void CreateFile(string relativePath)
        {
            var path = Path.Combine(Root, relativePath);
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, string.Empty);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
