using System.Text.RegularExpressions;

namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

/// <summary>
///     Helper methods for Blazor code-behind enforcement tests.
///     Provides utility functions for file system operations and path management.
/// </summary>
public sealed partial class BlazorCodeBehindEnforcementTests
{
    /// <summary>
    ///     Gets the root directory of the project by traversing up from the current directory.
    /// </summary>
    /// <returns>The absolute path to the project root directory.</returns>
    private static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        // Traverse up to find the solution root (contains .git or .sln file)
        while (directory != null &&
               directory.GetFiles("*.sln").Length == 0 &&
               directory.GetDirectories(".git").Length == 0)
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root directory");
    }

    /// <summary>
    ///     Retrieves all .razor files from the Blazor project directory.
    /// </summary>
    /// <returns>An enumerable of absolute file paths to .razor files.</returns>
    private static IEnumerable<string> GetAllRazorFiles()
    {
        if (!Directory.Exists(BlazorProjectPath)) throw new DirectoryNotFoundException($"Blazor project directory not found: {BlazorProjectPath}");

        return Directory.GetFiles(BlazorProjectPath, "*.razor", SearchOption.AllDirectories)
            .Where(file => !IsExcludedFile(file));
    }

    /// <summary>
    ///     Retrieves all .razor.cs code-behind files from the Blazor project directory.
    /// </summary>
    /// <returns>An enumerable of absolute file paths to .razor.cs files.</returns>
    private static IEnumerable<string> GetAllCodeBehindFiles()
    {
        if (!Directory.Exists(BlazorProjectPath)) throw new DirectoryNotFoundException($"Blazor project directory not found: {BlazorProjectPath}");

        return Directory.GetFiles(BlazorProjectPath, "*.razor.cs", SearchOption.AllDirectories)
            .Where(file => !IsExcludedFile(file));
    }

    /// <summary>
    ///     Determines if a file should be excluded from code-behind enforcement.
    ///     Excludes generated files, temporary files, and special framework files.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <returns>True if the file should be excluded, false otherwise.</returns>
    private static bool IsExcludedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var relativePath = GetRelativePath(filePath);

        // Exclude patterns
        var excludePatterns = new[]
        {
            "App.razor", // Root application component
            "_Imports.razor", // Blazor imports file
            "*.g.razor", // Generated files
            "*.generated.razor", // Generated files
            "bin", // Build output
            "obj", // Build intermediate files
            "wwwroot" // Static files
        };

        return excludePatterns.Any(pattern =>
            fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            (pattern.Contains('*') && MatchesWildcard(fileName, pattern)) ||
            relativePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Converts an absolute file path to a relative path from the project root.
    /// </summary>
    /// <param name="absolutePath">The absolute file path.</param>
    /// <returns>The relative path from the project root.</returns>
    private static string GetRelativePath(string absolutePath)
    {
        var projectRoot = GetProjectRoot();
        return Path.GetRelativePath(projectRoot, absolutePath).Replace('\\', '/');
    }

    /// <summary>
    ///     Checks if a filename matches a wildcard pattern.
    ///     Supports simple wildcard patterns with * for any characters.
    /// </summary>
    /// <param name="fileName">The filename to check.</param>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <returns>True if the filename matches the pattern, false otherwise.</returns>
    private static bool MatchesWildcard(string fileName, string pattern)
    {
        if (!pattern.Contains('*')) return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }
}