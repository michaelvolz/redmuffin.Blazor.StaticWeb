namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class SlnxProjectDiscoveryTests
{
    [Test]
    public async Task Discover_from_tools_directory_finds_tools_slnx()
    {
        var repoRoot = GetRepoRoot();
        var toolsDir = Path.Combine(repoRoot, "tools");

        var result = SlnxProjectDiscovery.Discover(toolsDir);

        await Assert.That(result).IsNotNull();
        await Assert.That(Path.GetFileName(result.SlnxPath))
            .IsEqualTo("redmuffin.Tools.slnx");
        await Assert.That(result.SourceProjects.Count).IsEqualTo(1);
        await Assert.That(result.TestProjects.Count).IsEqualTo(1);
        await Assert.That(result.SourceProjects[0])
            .EndsWith("redmuffin.Tools.QualityGates.csproj");
        await Assert.That(result.TestProjects[0])
            .EndsWith("redmuffin.Tools.QualityGates.Tests.csproj");
    }

    [Test]
    public async Task Discover_from_repo_root_finds_main_slnx()
    {
        var repoRoot = GetRepoRoot();

        var result = SlnxProjectDiscovery.Discover(repoRoot);

        await Assert.That(result).IsNotNull();
        await Assert.That(Path.GetFileName(result.SlnxPath))
            .IsEqualTo("redmuffin.Blazor.StaticWeb.slnx");
        await Assert.That(result.SourceProjects.Count).IsEqualTo(4);
        await Assert.That(result.TestProjects.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Discover_walks_up_to_find_slnx()
    {
        var repoRoot = GetRepoRoot();
        var deepDir = Path.Combine(repoRoot, "tools", "src",
            "redmuffin.Tools.QualityGates");

        var result = SlnxProjectDiscovery.Discover(deepDir);

        await Assert.That(result).IsNotNull();
        await Assert.That(Path.GetFileName(result.SlnxPath))
            .IsEqualTo("redmuffin.Tools.slnx");
    }

    [Test]
    public async Task Discover_with_null_directory_uses_current_directory()
    {
        var result = SlnxProjectDiscovery.Discover(null);

        // Should find either the main or tools .slnx depending on CWD
        await Assert.That(result).IsNotNull();
        await Assert.That(result.SourceProjects.Count).IsPositive();
    }

    [Test]
    public async Task Discover_non_existent_directory_throws()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
        {
            _ = SlnxProjectDiscovery.Discover("/tmp/nonexistent-8356129a");
        });
    }

    [Test]
    public async Task DiscoverFromSlnx_with_explicit_path_parses_correctly()
    {
        var repoRoot = GetRepoRoot();
        var slnxPath = Path.Combine(repoRoot,
            "redmuffin.Blazor.StaticWeb.slnx");

        var result = SlnxProjectDiscovery.DiscoverFromSlnx(slnxPath);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.SourceProjects.Count).IsEqualTo(4);
        await Assert.That(result.TestProjects.Count).IsEqualTo(2);
        await Assert.That(result.SlnxPath).EndsWith(
            "redmuffin.Blazor.StaticWeb.slnx");
    }

    [Test]
    public async Task DiscoverFromSlnx_nonexistent_file_throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            _ = SlnxProjectDiscovery.DiscoverFromSlnx(
                "/tmp/nonexistent-solution.slnx");
        });
    }

    [Test]
    public async Task DiscoverFromSlnx_resolves_relative_paths()
    {
        var repoRoot = GetRepoRoot();
        var result = SlnxProjectDiscovery.DiscoverFromSlnx(
            Path.Combine(repoRoot,
                "redmuffin.Blazor.StaticWeb.slnx"));

        // All resolved paths should be absolute
        foreach (var project in result.SourceProjects)
            await Assert.That(Path.IsPathRooted(project)).IsTrue();
        foreach (var project in result.TestProjects)
            await Assert.That(Path.IsPathRooted(project)).IsTrue();
    }

    [Test]
    public async Task DiscoverFromSlnx_tools_solution_finds_quality_gates()
    {
        var repoRoot = GetRepoRoot();
        var slnxPath = Path.Combine(repoRoot, "tools",
            "redmuffin.Tools.slnx");

        var result = SlnxProjectDiscovery.DiscoverFromSlnx(slnxPath);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.SourceProjects.Count).IsEqualTo(1);
        await Assert.That(result.TestProjects.Count).IsEqualTo(1);
        await Assert.That(result.SourceProjects[0])
            .EndsWith("redmuffin.Tools.QualityGates.csproj");
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null &&
               !File.Exists(Path.Combine(dir,
                   "redmuffin.Blazor.StaticWeb.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ??
               throw new InvalidOperationException(
                   "Could not find repo root from test binary location.");
    }
}
