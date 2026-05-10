namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     CLI command for the dupes (duplicate code detection) gate.
///     Replicates Uncle Bob's dry4clj tool.
/// </summary>
public static class DupesCommand
{
    public static Command Create()
    {
        var pathsArg = new Argument<string[]>("paths")
        {
            Description = "Files or directories to scan for duplicates",
        };

        var thresholdOption = new Option<double>("--threshold")
        {
            Description = "Minimum structural similarity score (default 0.82)",
        };

        var minLinesOption = new Option<int>("--min-lines")
        {
            Description = "Minimum source lines in a candidate form (default 4)",
        };

        var minNodesOption = new Option<int>("--min-nodes")
        {
            Description = "Minimum normalized syntax nodes (default 20)",
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text or json (default text)",
        };

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Same as --format json",
        };

        var command = new Command("dupes", "Find structural duplicate code candidates")
        {
            pathsArg,
            thresholdOption,
            minLinesOption,
            minNodesOption,
            formatOption,
            jsonOption,
        };

        command.SetAction(parseResult =>
        {
            var paths = parseResult.GetValue(pathsArg) ?? [];
            var threshold = parseResult.GetValue(thresholdOption);
            if (threshold <= 0) threshold = 0.82;
            var minLines = parseResult.GetValue(minLinesOption);
            if (minLines <= 0) minLines = 4;
            var minNodes = parseResult.GetValue(minNodesOption);
            if (minNodes <= 0) minNodes = 20;
            var format = parseResult.GetValue(formatOption) ?? "text";
            var json = parseResult.GetValue(jsonOption);

            if (json) format = "json";

            var options = new DupesOptions(
                Threshold: threshold,
                MinLines: minLines,
                MinNodes: minNodes,
                Format: format,
                Paths: [.. paths]);

            var (exitCode, candidates) = DupesHandler.Run(options);
            Console.WriteLine(DupesOutputFormatter.Format(candidates, format));
            Environment.ExitCode = exitCode;
        });

        return command;
    }
}
