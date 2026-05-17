namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using System.IO;
using redmuffin.Tools.QualityGates.Analysis;

public static class DepthCommand
{
    private static readonly Option<DirectoryInfo> ProjectOption = new("--project")
    {
        Description = "Path to the project directory to analyze",
        Required = true,
    };

    private static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Write per-file analysis summary",
    };

    private static readonly Func<ParseResult, int> DeepAction = parseResult =>
    {
        var projectPath = parseResult.GetValue(ProjectOption)!.FullName;
        var verbose = parseResult.GetValue(VerboseOption);

        return Execute(projectPath, verbose);
    };

    public static Command Create()
    {
        var command = new Command(
            "depth",
            "Analyze structural depth: shallow methods, parameter bloat, wrong abstractions, and entanglement.")
        {
            ProjectOption,
            VerboseOption,
        };

        command.SetAction(DeepAction);
        return command;
    }

    public static int Execute(string projectPath, bool verbose = false, TextWriter? output = null)
    {
        output ??= Console.Out;

        if (!Directory.Exists(projectPath))
        {
            output.WriteLine($"Project directory not found: {projectPath}");
            return 1;
        }

        if (verbose)
        {
            output.WriteLine($"Analyzing structural depth in: {projectPath}");
        }

        var results = DepthDetector.Analyze(projectPath);

        if (verbose)
        {
            var byFile = results
                .GroupBy(r => r.FilePath, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal);

            foreach (var fileGroup in byFile)
            {
                var fileName = Path.GetFileName(fileGroup.Key);
                output.WriteLine($"  {fileName}: {fileGroup.Count()} method(s) with issues");
            }
        }

        return DepthHandler.Run(results, output: output);
    }
}
