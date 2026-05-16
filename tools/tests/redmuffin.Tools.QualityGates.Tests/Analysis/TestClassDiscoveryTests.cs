namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;
using TUnit.Core;

public sealed class TestClassDiscoveryTests
{
    [Test]
    public async Task should_discover_test_class_when_matching_file_exists()
    {
        var testProject = ResolveTestProjectPath();
        var sourcePath = Path.Combine(testProject, "..", "..", "src",
            "redmuffin.Tools.QualityGates", "Commands", "CrapCommand.cs");

        var result = TestClassDiscovery.Discover(sourcePath, testProject);

        await Assert.That(result).IsEqualTo("CrapCommandTests");
    }

    [Test]
    public async Task should_return_null_when_no_matching_test_file_exists()
    {
        var testProject = ResolveTestProjectPath();
        var sourcePath = Path.Combine(testProject, "NoSuchSource.cs");

        var result = TestClassDiscovery.Discover(sourcePath, testProject);

        await Assert.That(result).IsNull();
    }

    private static string ResolveTestProjectPath()
    {
        // Walk up from the test binary directory to find the test project root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "redmuffin.Tools.QualityGates.Tests.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find test project root");
    }
}
