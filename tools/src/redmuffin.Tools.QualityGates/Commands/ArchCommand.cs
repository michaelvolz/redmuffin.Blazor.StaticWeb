namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Models;

public static class ArchCommand
{
    public static Command Create()
    {
        var projectOption = new Option<string>("--project")
        {
            Description = "Path to the solution or project root to scan",
            Required = true,
        };

        var configOption = new Option<string>("--architecture-config")
        {
            Description = "Path to the YAML architecture config file",
            Required = true,
        };

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output results as JSON",
        };

        var command = new Command(
            "architecture",
            "Check project dependency architecture against component rules")
        {
            projectOption,
            configOption,
            jsonOption,
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(projectOption)!;
            var configPath = parseResult.GetValue(configOption)!;
            var json = parseResult.GetValue(jsonOption);

            return Execute(projectPath, configPath, json);
        });

        return command;
    }

    public static int Execute(string projectPath, string configPath, bool json)
    {
        var (exitCode, result) = ArchHandler.Run(configPath, projectPath);
        var output = ArchOutputFormatter.Format(result, json);
        Console.WriteLine(output);
        return exitCode;
    }
}
