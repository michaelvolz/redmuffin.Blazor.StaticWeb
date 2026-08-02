namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class SourcePathFilterTests
{
    [Test]
    [Arguments("src/App/File.cs", true)]
    [Arguments("src\\App\\File.cs", true)]
    [Arguments("WrongAbstraction.cs", true)]
    [Arguments("src/App/bin/Debug/File.cs", false)]
    [Arguments("src\\App\\bin\\Debug\\File.cs", false)]
    [Arguments("src/App/obj/Debug/File.cs", false)]
    [Arguments("src\\App\\obj\\Debug\\File.cs", false)]
    [Arguments("bin/Debug/File.cs", false)]
    [Arguments("bin\\Debug\\File.cs", false)]
    [Arguments("obj/Debug/File.cs", false)]
    public async Task IsSourcePath_classifies_build_output_segments(string path, bool expected)
    {
        await Assert.That(SourcePathFilter.IsSourcePath(path)).IsEqualTo(expected);
    }

    [Test]
    public async Task EnumerateCsFiles_analyzes_when_scan_root_is_under_bin()
    {
        var root = Path.Combine(Path.GetTempPath(), $"source_path_binroot_{Guid.NewGuid():N}", "bin", "Debug", "fixtures");
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "Fixture.cs"), "class Fixture {}").ConfigureAwait(false);

            var files = SourcePathFilter.EnumerateCsFiles(root)
                .Select(static path => Path.GetFileName(path)!)
                .ToArray();

            await Assert.That(files).HasSingleItem();
            await Assert.That(files[0]).IsEqualTo("Fixture.cs");
        }
        finally
        {
            var parent = Directory.GetParent(root)?.Parent?.Parent?.FullName;
            if (parent is not null && Directory.Exists(parent))
                Directory.Delete(parent, recursive: true);
        }
    }

    [Test]
    public async Task EnumerateCsFiles_skips_bin_and_obj_trees()
    {
        var root = Path.Combine(Path.GetTempPath(), $"source_path_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            Directory.CreateDirectory(Path.Combine(root, "bin", "Debug"));
            Directory.CreateDirectory(Path.Combine(root, "obj", "Debug"));
            await File.WriteAllTextAsync(Path.Combine(root, "src", "Real.cs"), "class Real {}").ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(root, "bin", "Debug", "Copy.cs"), "class Copy {}").ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(root, "obj", "Debug", "Gen.cs"), "class Gen {}").ConfigureAwait(false);

            var files = SourcePathFilter.EnumerateCsFiles(root)
                .Select(static path => Path.GetFileName(path)!)
                .ToArray();

            await Assert.That(files).HasSingleItem();
            await Assert.That(files[0]).IsEqualTo("Real.cs");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task FindTests_skips_tests_under_bin()
    {
        var root = Path.Combine(Path.GetTempPath(), $"scrap_bin_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Tests"));
            Directory.CreateDirectory(Path.Combine(root, "bin", "Debug", "net10.0", "Fixtures"));
            await File.WriteAllTextAsync(Path.Combine(root, "Tests", "RealTests.cs"), """
                using TUnit.Core;
                public class RealTests { [Test] public void real_test() { } }
                """).ConfigureAwait(false);
            await File.WriteAllTextAsync(
                Path.Combine(root, "bin", "Debug", "net10.0", "Fixtures", "CopiedTests.cs"),
                """
                using TUnit.Core;
                public class CopiedTests { [Test] public void copied_test() { } }
                """).ConfigureAwait(false);

            var results = TestMethodParser.FindTests(root);

            await Assert.That(results.Count).IsEqualTo(1);
            await Assert.That(results[0].MethodName).IsEqualTo("real_test");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
