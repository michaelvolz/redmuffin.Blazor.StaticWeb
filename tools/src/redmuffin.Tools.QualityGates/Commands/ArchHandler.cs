using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Models;

namespace redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Orchestrator for the Architecture gate.
///     Reads the YAML config, builds the project dependency graph,
///     delegates analysis to ArchAnalyzer, and produces the gate result.
/// </summary>
public static class ArchHandler
{
    public static (int ExitCode, ArchResult Result) Run(string configPath, string projectPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                return (1, new ArchResult(1, [], [], 0, 0));
            }

            var yaml = File.ReadAllText(configPath);
            var result = RunConfigPipeline(yaml, projectPath);
            return (result.ExitCode, result);
        }
        catch (DirectoryNotFoundException)
        {
            return (1, new ArchResult(1, [], [], 0, 0));
        }
        catch (FormatException ex)
        {
            Console.Error.WriteLine($"Error parsing architecture config: {ex.Message}");
            return (1, new ArchResult(1, [], [], 0, 0));
        }
    }

    public static ArchResult RunConfigPipeline(string yaml, string projectPath)
    {
        var config = ArchConfig.Parse(yaml);
        var projectGraph = ProjectGraph.From(projectPath);
        var componentGraph = ComponentGraph.From(projectGraph, config);
        var violations = ArchAnalyzer.FindViolations(componentGraph, config);
        var cycles = ArchAnalyzer.FindCycles(componentGraph);

        var result = new ArchResult(
            0,
            violations,
            cycles,
            projectGraph.Dependencies.Count,
            config.ComponentMap.Count);

        var exitCode = ArchAnalyzer.DecideExitCode(violations, cycles, config);
        return result with { ExitCode = exitCode };
    }
}
