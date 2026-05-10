namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     CLI command for the dupes (duplicate code detection) gate.
///     Replicates Uncle Bob's dry4clj tool.
/// </summary>
public static class DupesCommand
{
    private static readonly Argument<string[]> PathsArg = new("paths")
    {
        Description = "Files or directories to scan for duplicates",
    };

    private static readonly Option<double> ThresholdOption = new("--threshold")
    {
        Description = "Minimum structural similarity score (default 0.82)",
    };

    private static readonly Option<int> MinLinesOption = new("--min-lines")
    {
        Description = "Minimum source lines in a candidate form (default 4)",
    };

    private static readonly Option<int> MinNodesOption = new("--min-nodes")
    {
        Description = "Minimum normalized syntax nodes (default 20)",
    };

    private static readonly Option<string> FormatOption = new("--format")
    {
        Description = "Output format: text or json (default text)",
    };

    private static readonly Option<bool> JsonOption = new("--json")
    {
        Description = "Same as --format json",
    };

    public static Command Create()
    {
        var command = new Command("dupes", "Find structural duplicate code candidates")
        {
            PathsArg, ThresholdOption, MinLinesOption, MinNodesOption, FormatOption, JsonOption,
        };

        command.SetAction(parseResult =>
        {
            var paths = parseResult.GetValue(PathsArg) ?? [];
            var format = parseResult.GetValue(FormatOption) ?? "text";

            if (parseResult.GetValue(JsonOption))
            {
                format = "json";
            }

            var options = new DupesOptions(
                Threshold: ApplyDefault(parseResult.GetValue(ThresholdOption), 0.82),
                MinLines: ApplyDefault(parseResult.GetValue(MinLinesOption), 4),
                MinNodes: ApplyDefault(parseResult.GetValue(MinNodesOption), 20),
                Format: format,
                Paths: [.. paths]);

            var (exitCode, candidates) = DupesHandler.Run(options);
            Console.WriteLine(DupesOutputFormatter.Format(candidates, format));
            Environment.ExitCode = exitCode;
        });

        return command;
    }

    private static double ApplyDefault(double value, double defaultValue) => value > 0 ? value : defaultValue;

    private static int ApplyDefault(int value, int defaultValue) => value > 0 ? value : defaultValue;
}
