namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     Filters filesystem paths so analysis gates only see source, not build output.
///     Build-output checks use the path relative to the scan root so fixtures
///     that happen to live under a test project's bin folder are still analyzed
///     when that folder is the scan root.
/// </summary>
public static class SourcePathFilter
{
    public static bool IsUnderBuildOutput(string path)
    {
        foreach (var segment in path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("bin", StringComparison.Ordinal)
                || segment.Equals("obj", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsSourcePath(string path) => !IsUnderBuildOutput(path);

    public static IEnumerable<string> EnumerateCsFiles(string directory)
    {
        var root = Path.GetFullPath(directory);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsSourcePath(Path.GetRelativePath(root, file)));
    }
}
