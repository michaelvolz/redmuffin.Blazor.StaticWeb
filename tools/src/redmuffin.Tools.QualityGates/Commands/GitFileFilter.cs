namespace redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Shared helpers for git-based file filtering across commands.
/// </summary>
internal static class GitFileFilter
{
    /// <summary>
    ///     Filters methods to only those in files modified since HEAD.
    /// </summary>
    public static IReadOnlyList<T> FilterChanged<T>(
        IReadOnlyList<T> methods,
        string projectPath,
        Func<T, string> filePathSelector)
    {
        var changedFiles = GetChangedFiles(projectPath);
        if (changedFiles is null)
        {
            return methods;
        }

        var changedSet = new HashSet<string>(changedFiles, StringComparer.OrdinalIgnoreCase);
        return methods
            .Where(m => changedSet.Contains(filePathSelector(m)))
            .ToList()
            .AsReadOnly();
    }

    private static HashSet<string>? GetChangedFiles(string projectPath)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "diff HEAD --name-only",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return null;
            }

            var files = output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetFullPath(f, projectPath));

            return new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }
}
