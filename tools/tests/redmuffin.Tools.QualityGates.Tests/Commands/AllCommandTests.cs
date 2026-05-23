namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Unit tests for AllCommand helpers.
/// </summary>
public sealed class AllCommandTests
{
    // ── RunGatesAsync ──

    [Test]
    public async Task RunGatesAsync_skipped_gate_writes_skip_and_returns_zero()
    {
        using var writer = new StringWriter();
        var gates = new GateDescriptor[]
        {
            new("Test Gate", () => Task.FromResult(2), Skip: true),
        };

        var results = await AllCommand.RunGatesAsync(writer, gates).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].ExitCode).IsEqualTo(0);
        await Assert.That(results[0].Skipped).IsTrue();
        await Assert.That(writer.ToString()).Contains("SKIPPED");
    }

    [Test]
    public async Task RunGatesAsync_running_gate_returns_its_exit_code()
    {
        using var writer = new StringWriter();
        var gates = new GateDescriptor[]
        {
            new("Test Gate", () => Task.FromResult(2), Skip: false),
        };

        var results = await AllCommand.RunGatesAsync(writer, gates).ConfigureAwait(false);

        await Assert.That(results[0].ExitCode).IsEqualTo(2);
        await Assert.That(results[0].Skipped).IsFalse();
        await Assert.That(writer.ToString()).DoesNotContain("SKIPPED");
    }

    [Test]
    public async Task RunGatesAsync_multiple_gates_collects_all_results()
    {
        using var writer = new StringWriter();
        var gates = new GateDescriptor[]
        {
            new("Gate A", () => Task.FromResult(0), Skip: false),
            new("Gate B", () => Task.FromResult(2), Skip: false),
            new("Gate C", () => Task.FromResult(0), Skip: true),
        };

        var results = await AllCommand.RunGatesAsync(writer, gates).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results[0].ExitCode).IsEqualTo(0);
        await Assert.That(results[1].ExitCode).IsEqualTo(2);
        await Assert.That(results[2].Skipped).IsTrue();
    }

    [Test]
    public async Task RunGatesAsync_skipped_gate_appears_in_summary_as_na()
    {
        using var writer = new StringWriter();
        var gates = new GateDescriptor[]
        {
            new("Architecture", () => Task.FromResult(0), Skip: false),
            new("Depth", () => Task.FromResult(0), Skip: true),
        };

        await AllCommand.RunGatesAsync(writer, gates).ConfigureAwait(false);

        var output = writer.ToString();
        await Assert.That(output).Contains("Architecture: PASS");
        await Assert.That(output).Contains("Depth: N/A");
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
