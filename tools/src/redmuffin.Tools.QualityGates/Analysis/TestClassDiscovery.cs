namespace redmuffin.Tools.QualityGates.Analysis;

public static class TestClassDiscovery
{
    public static string? Discover(string sourcePath, string testProjectPath)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
        var testFileName = sourceFileName + "Tests.cs";

        var testFiles = Directory.GetFiles(testProjectPath, testFileName, SearchOption.AllDirectories);

        return testFiles.Length > 0
            ? Path.GetFileNameWithoutExtension(testFiles[0])
            : null;
    }
}
