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

    private static readonly Action<ParseResult> DupesAction = parseResult =>
    {
        var paths = parseResult.GetValue(PathsArg) ?? [];
        var format = ResolveFormat(parseResult.GetValue(JsonOption), parseResult.GetValue(FormatOption));

        var options = new DupesOptions(
            Threshold: ApplyDefault(parseResult.GetValue(ThresholdOption), 0.82),
            MinLines: ApplyDefault(parseResult.GetValue(MinLinesOption), 4),
            MinNodes: ApplyDefault(parseResult.GetValue(MinNodesOption), 20),
            Format: format,
            Paths: [.. paths]);

        var (exitCode, candidates) = DupesHandler.Run(options);
        Console.WriteLine(DupesOutputFormatter.Format(candidates, format));
        Environment.ExitCode = exitCode;
    };

    public static Command Create()
    {
        var command = new Command("duplicates", "Find structural duplicate code candidates")
        {
            PathsArg, ThresholdOption, MinLinesOption, MinNodesOption, FormatOption, JsonOption,
        };

        command.SetAction(DupesAction);
        return command;
    }

    public static string ResolveFormat(bool json, string? formatOption) =>
        json ? "json" : formatOption ?? "text";

    public static double ApplyDefault(double value, double defaultValue) => value > 0 ? value : defaultValue;

    public static int ApplyDefault(int value, int defaultValue) => value > 0 ? value : defaultValue;
}
