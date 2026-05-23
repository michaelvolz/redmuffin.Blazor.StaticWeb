namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Parsed command-line arguments for the ConfigureAwaitFixer.
/// </summary>
public sealed class Arguments
{
    private Arguments(string? singleFile, string targetDir, string projectPath)
    {
        SingleFile = singleFile;
        TargetDir = targetDir;
        ProjectPath = projectPath;
    }

    public string? SingleFile { get; }
    public string TargetDir { get; }
    public string ProjectPath { get; }

    /// <summary>
    ///     Returns true if running in a CI environment — the fixer should be skipped.
    /// </summary>
    public static bool IsRunningInCI() =>
        string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Parses the --file flag from raw args and discovers the .csproj path
    ///     by walking up from the target directory.
    ///     Returns null if no .csproj is found.
    /// </summary>
    public static Arguments? Parse(string[] args)
    {
        string? singleFile = null;
        var targetDir = Environment.CurrentDirectory;

        // Parse --file <path> flag
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--file", StringComparison.Ordinal))
            {
                if (i + 1 < args.Length)
                {
                    singleFile = Path.GetFullPath(args[i + 1]);
                    targetDir = Path.GetDirectoryName(singleFile)!;
                    i++;
                }
                else
                {
                    Console.Error.WriteLine("--file requires a path argument");
                    return null;
                }
            }
            else
            {
                targetDir = args[i];
            }
        }

        // Walk up from targetDir to find .csproj
        var csprojFiles = new List<string>();
        var searchDir = targetDir;
        while (searchDir is not null)
        {
            csprojFiles.AddRange(Directory.EnumerateFiles(searchDir, "*.csproj", SearchOption.TopDirectoryOnly));
            if (csprojFiles.Count > 0)
                break;
            searchDir = Path.GetDirectoryName(searchDir);
        }

        if (csprojFiles.Count == 0)
        {
            Console.Error.WriteLine($"No .csproj found above {targetDir}");
            return null;
        }

        return new Arguments(singleFile, targetDir, csprojFiles[0]);
    }
}
