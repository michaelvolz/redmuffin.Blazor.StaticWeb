using redmuffin.Tools.QualityGates.Models;

namespace redmuffin.Tools.QualityGates.Commands;

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
        var violations = FindViolations(componentGraph, config);
        var cycles = FindCycles(componentGraph);

        var result = new ArchResult(
            0,
            violations,
            cycles,
            projectGraph.Dependencies.Count,
            config.ComponentMap.Count);

        var exitCode = DecideExitCode(violations, cycles, config);
        return result with { ExitCode = exitCode };
    }

    public static int DecideExitCode(
        IReadOnlyList<ArchViolation> violations,
        IReadOnlyList<ArchCycle> cycles,
        ArchConfig config)
    {
        if (config.FailOnViolations && violations.Count > 0)
        {
            return 2;
        }

        if (config.FailOnCycles && cycles.Count > 0)
        {
            return 2;
        }

        return 0;
    }

    public static IReadOnlyList<ArchViolation> FindViolations(
        ComponentGraph graph, ArchConfig config)
    {
        var violations = new List<ArchViolation>();

        foreach (var unmappedProject in graph.UnmappedProjects)
        {
            violations.Add(new ArchViolation(
                unmappedProject, string.Empty,
                "Default", string.Empty,
                $"Project '{unmappedProject}' is not assigned to any component."));
        }

        foreach (var (sourceComp, targets) in graph.Dependencies)
        {
            var allowed = new HashSet<string>(
                config.AllowedDependencies.GetValueOrDefault(sourceComp, []),
                StringComparer.Ordinal);

            foreach (var targetComp in targets)
            {
                if (!allowed.Contains(targetComp))
                {
                    violations.Add(new ArchViolation(
                        string.Empty, string.Empty,
                        sourceComp, targetComp,
                        $"{sourceComp} is not allowed to depend on {targetComp}."));
                }
            }
        }

        return violations;
    }

    public static IReadOnlyList<ArchCycle> FindCycles(ComponentGraph graph)
    {
        var cycles = new List<ArchCycle>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var node in graph.Dependencies.Keys)
        {
            if (!visited.Contains(node))
            {
                Dfs(node, graph.Dependencies, visited, stack, path, cycles);
            }
        }

        return cycles;
    }

    private static void Dfs(
        string node,
        IReadOnlyDictionary<string, ISet<string>> adj,
        HashSet<string> visited,
        HashSet<string> stack,
        List<string> path,
        List<ArchCycle> cycles)
    {
        visited.Add(node);
        stack.Add(node);
        path.Add(node);

        if (adj.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    Dfs(neighbor, adj, visited, stack, path, cycles);
                }
                else if (stack.Contains(neighbor))
                {
                    var cycleStart = path.IndexOf(neighbor);
                    if (cycleStart >= 0)
                    {
                        var cycle = path.GetRange(cycleStart, path.Count - cycleStart);
                        var normalized = NormalizeCycle(cycle);
                        if (!cycles.Exists(c => c.Components.SequenceEqual(normalized, StringComparer.Ordinal)))
                        {
                            cycles.Add(new ArchCycle(normalized, normalized.Count));
                        }
                    }
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        stack.Remove(node);
    }

    private static List<string> NormalizeCycle(List<string> cycle)
    {
        var minIndex = cycle.IndexOf(cycle.Min(StringComparer.Ordinal)!);
        var normalized = new List<string>();
        for (var i = 0; i < cycle.Count; i++)
        {
            normalized.Add(cycle[(minIndex + i) % cycle.Count]);
        }

        return normalized;
    }
}
