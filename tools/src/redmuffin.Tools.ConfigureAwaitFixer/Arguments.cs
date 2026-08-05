namespace redmuffin.Tools.ConfigureAwaitFixer;

/// <summary>
///     Parsed command-line arguments for the ConfigureAwaitFixer.
/// </summary>
public sealed class Arguments
{
    private Arguments(
        FixerMode mode,
        string? singleFile,
        string targetDir,
        string projectPath,
        string? instance,
        string? logPath,
        string? idleSeconds)
    {
        Mode = mode;
        SingleFile = singleFile;
        TargetDir = targetDir;
        ProjectPath = projectPath;
        Instance = instance;
        LogPath = logPath;
        IdleSeconds = idleSeconds;
    }

    public FixerMode Mode { get; }
    public string? SingleFile { get; }
    public string TargetDir { get; }
    public string ProjectPath { get; }

    /// <summary>
    ///     Pipe/mutex isolation suffix for a daemon spawned detached by the
    ///     client (the client forwards its own environment as explicit args).
    /// </summary>
    public string? Instance { get; }

    /// <summary>
    ///     Log file path for a daemon spawned detached by the client.
    /// </summary>
    public string? LogPath { get; }

    /// <summary>
    ///     Idle-exit timeout in seconds for a daemon spawned detached by the
    ///     client.
    /// </summary>
    public string? IdleSeconds { get; }

    /// <summary>
    ///     Returns true if running in a CI environment — the fixer should be skipped.
    /// </summary>
    public static bool IsRunningInCI() =>
        string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Parses the command-line arguments: <c>--fix &lt;file&gt;</c> hands a
    ///     single file to the daemon, <c>--daemon</c> starts the daemon server,
    ///     and the legacy forms (positional target directory or
    ///     <c>--file &lt;path&gt;</c>) run the one-shot pipeline. Returns null
    ///     when a required argument is missing or, for the one-shot mode, when
    ///     no .csproj is found above the target directory.
    /// </summary>
    public static Arguments? Parse(string[] args)
    {
        string? singleFile = null;
        string? instance = null, logPath = null, idleSeconds = null;
        var targetDir = Environment.CurrentDirectory;
        var mode = FixerMode.OneShot;

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--file", StringComparison.Ordinal))
            {
                if (!TryReadValue(args, ref i, "--file", out var file))
                    return null;
                singleFile = Path.GetFullPath(file);
                targetDir = Path.GetDirectoryName(singleFile)!;
            }
            else if (string.Equals(args[i], "--fix", StringComparison.Ordinal))
            {
                if (!TryReadValue(args, ref i, "--fix", out var file))
                    return null;
                mode = FixerMode.Fix;
                singleFile = Path.GetFullPath(file);
            }
            else if (string.Equals(args[i], "--daemon", StringComparison.Ordinal))
            {
                mode = FixerMode.Daemon;
            }
            else if (string.Equals(args[i], "--instance", StringComparison.Ordinal))
            {
                if (!TryReadValue(args, ref i, "--instance", out var value))
                    return null;
                instance = value;
            }
            else if (string.Equals(args[i], "--log", StringComparison.Ordinal))
            {
                if (!TryReadValue(args, ref i, "--log", out var value))
                    return null;
                logPath = value;
            }
            else if (string.Equals(args[i], "--idle", StringComparison.Ordinal))
            {
                if (!TryReadValue(args, ref i, "--idle", out var value))
                    return null;
                idleSeconds = value;
            }
            else
            {
                targetDir = args[i];
            }
        }

        // Only the one-shot mode resolves the project at parse time.
        var projectPath = mode == FixerMode.OneShot ? FindCsproj(targetDir) : string.Empty;
        if (projectPath is null)
        {
            Console.Error.WriteLine($"No .csproj found above {targetDir}");
            return null;
        }

        return new Arguments(mode, singleFile, targetDir, projectPath, instance, logPath, idleSeconds);
    }

    /// <summary>
    ///     Returns the first .csproj in <paramref name="targetDir"/> or its nearest
    ///     ancestor, or null if no .csproj exists anywhere above it.
    /// </summary>
    public static string? FindCsproj(string targetDir)
    {
        var searchDir = targetDir;
        while (searchDir is not null)
        {
            var csprojFiles = Directory.EnumerateFiles(searchDir, "*.csproj", SearchOption.TopDirectoryOnly)
                .ToList();
            if (csprojFiles.Count > 0)
                return csprojFiles[0];
            searchDir = Path.GetDirectoryName(searchDir);
        }

        return null;
    }

    /// <summary>
    ///     Consumes the value following <paramref name="flag"/> and advances the
    ///     parse index past it. Reports a loud error and returns false when the
    ///     flag is the last token (dangling flag).
    /// </summary>
    private static bool TryReadValue(string[] args, ref int index, string flag, out string value)
    {
        if (index + 1 < args.Length)
        {
            value = args[index + 1];
            index++;
            return true;
        }

        Console.Error.WriteLine($"{flag} requires a value");
        value = string.Empty;
        return false;
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-04T19:45:07.4428534Z","moduleHash":"7e984aaadd6ee54f1d7dea2c75e3f32c5b9b03778120ad54207eda0d37d9ec80","forms":[{"id":"IsRunningInCI","line":23,"endLine":27,"hash":"cac95c11498b8f14265051d35d2993c0219e9684d6d7b7e2bc632f5da5a3db38"},{"id":"Parse","line":37,"endLine":97,"hash":"25569861552e1f8461939c2f6d34f70bf48ba13cb99f90356547aac7f62a936c"},{"id":"FindCsproj","line":103,"endLine":116,"hash":"b8532c6381190c1b16fa8a464dc85ad4c9c132b7dc71da4744c5b8784a9c4d7d"}]}
// clj-mutate-manifest-end
