namespace redmuffin.Tools.QualityGates.Analysis;

using redmuffin.Tools.QualityGates.Models;

/// <summary>
///     Pure analysis functions for the Architecture gate:
///     violation detection, cycle detection, and exit code decision.
///     No I/O — operates entirely on in-memory data structures.
/// </summary>
public static class ArchAnalyzer
{
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
