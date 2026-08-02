namespace redmuffin.Tools.QualityGates.Analysis;

public static class TestClassDiscovery
{
    public static string? Discover(string sourcePath, string testProjectPath)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
        var testFileName = sourceFileName + "Tests.cs";

        var testFiles = Directory.EnumerateFiles(testProjectPath, testFileName, SearchOption.AllDirectories)
            .Where(SourcePathFilter.IsSourcePath)
            .ToArray();

        return testFiles.Length > 0
            ? Path.GetFileNameWithoutExtension(testFiles[0])
            : null;
    }
}
