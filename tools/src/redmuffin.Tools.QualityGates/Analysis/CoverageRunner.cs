namespace redmuffin.Tools.QualityGates.Analysis;

using System.Diagnostics;
using System.Globalization;

/// <summary>
///     Shared coverage generation for CRAP and Mutation gates.
///     Spawns <c>dotnet run --coverage</c> against test projects
///     to produce Cobertura XML files.
/// </summary>
public static class CoverageRunner
{
    /// <summary>
    ///     Generates Cobertura coverage XML for a single test project.
    ///     Returns the path to the generated file, or null on failure.
    /// </summary>
    public static async Task<string?> GenerateAsync(string testProjectPath)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cobertura.xml");

        using var process = new Process
        {
            StartInfo = BuildStartInfo(testProjectPath, outputPath),
        };

        process.Start();
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

        if (!IsSuccessful(process.ExitCode, outputPath))
            return null;

        return outputPath;
    }

    /// <summary>
    ///     Synchronous wrapper around <see cref="GenerateAsync"/>.
    /// </summary>
    public static string? Generate(string testProjectPath)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cobertura.xml");

        using var process = new Process
        {
            StartInfo = BuildStartInfo(testProjectPath, outputPath),
        };

        process.Start();
        process.WaitForExit();

        if (!IsSuccessful(process.ExitCode, outputPath))
            return null;

        return outputPath;
    }

    public static ProcessStartInfo BuildStartInfo(string testProjectPath, string outputPath)
    {
        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{testProjectPath}\" --coverage --coverage-output-format cobertura --coverage-output \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }

    public static bool IsSuccessful(int exitCode, string outputPath) =>
        exitCode == 0 && File.Exists(outputPath);

    /// <summary>
    ///     Reports error details from a failed process to stderr.
    /// </summary>
    public static void ReportError(int exitCode, string? stderr)
    {
        Console.Error.WriteLine($"Failed to generate coverage. dotnet run exit code: {exitCode.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.Error.WriteLine(stderr);
        }
    }
}
