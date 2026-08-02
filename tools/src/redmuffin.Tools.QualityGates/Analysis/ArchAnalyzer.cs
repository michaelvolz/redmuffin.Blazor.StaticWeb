namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Models;

/// <summary>
///     Pure analysis functions for the Architecture gate:
///     violation detection, cycle detection, metrics/zones, and exit code decision.
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
            foreach (var targetComp in targets)
            {
                if (IsEdgeAllowed(sourceComp, targetComp, config))
                {
                    continue;
                }

                violations.Add(new ArchViolation(
                    string.Empty, string.Empty,
                    sourceComp, targetComp,
                    $"{sourceComp} is not allowed to depend on {targetComp}."));
            }
        }

        return violations;
    }

    /// <summary>
    /// dependency-checker edge rules: exceptions suppress; forbidden denies;
    /// allow-list or <c>all</c> permits; self-edges are always allowed by the graph builder.
    /// </summary>
    public static bool IsEdgeAllowed(string source, string target, ArchConfig config)
    {
        if (HasEdge(config.AllowedExceptions, source, target))
        {
            return true;
        }

        if (HasEdge(config.ForbiddenDependencies, source, target))
        {
            return false;
        }

        var allowed = config.AllowedDependencies.GetValueOrDefault(source, []);
        if (allowed.Any(a => string.Equals(a, "all", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return allowed.Contains(target, StringComparer.Ordinal);
    }

    private static bool HasEdge(
        IReadOnlyList<DependencyEdge> edges, string source, string target) =>
        edges.Any(e =>
            string.Equals(e.From, source, StringComparison.Ordinal)
            && string.Equals(e.To, target, StringComparison.Ordinal));

    /// <summary>
    /// Compute main-sequence metrics per component. Abstractness is derived by
    /// scanning source under <paramref name="projectPath"/> for projects mapped
    /// to each component (interfaces + abstract classes / all type declarations).
    /// </summary>
    public static IReadOnlyList<ComponentMetric> ComputeMetrics(
        ComponentGraph graph, ArchConfig config, string projectPath)
    {
        var components = CollectComponents(graph, config);
        var fanIn = ComputeFanIn(graph.Dependencies);
        var abstractness = ComputeAbstractnessByComponent(config, projectPath);
        var threshold = config.HealthyThreshold;
        var metrics = new List<ComponentMetric>(components.Count);

        foreach (var component in components.OrderBy(c => c, StringComparer.Ordinal))
        {
            var outCount = graph.Dependencies.TryGetValue(component, out var targets)
                ? targets.Count
                : 0;
            var inCount = fanIn.GetValueOrDefault(component, 0);
            var instability = inCount + outCount == 0
                ? 0.0
                : (double)outCount / (inCount + outCount);
            var a = abstractness.GetValueOrDefault(component, 0.0);
            var distance = Math.Abs(a + instability - 1.0);
            var zone = ClassifyZone(a + instability, threshold);

            metrics.Add(new ComponentMetric(
                component, inCount, outCount, instability, a, distance, zone));
        }

        return metrics;
    }

    public static ArchZone ClassifyZone(double abstractnessPlusInstability, double healthyThreshold)
    {
        var low = 1.0 - healthyThreshold;
        var high = 1.0 + healthyThreshold;
        if (abstractnessPlusInstability < low)
        {
            return ArchZone.Pain;
        }

        if (abstractnessPlusInstability > high)
        {
            return ArchZone.Useless;
        }

        return ArchZone.Healthy;
    }

    private static HashSet<string> CollectComponents(ComponentGraph graph, ArchConfig config)
    {
        var components = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in config.ComponentMap.Values)
        {
            if (!config.IgnoredComponents.Contains(component, StringComparer.Ordinal))
            {
                components.Add(component);
            }
        }

        foreach (var key in graph.Dependencies.Keys)
        {
            components.Add(key);
        }

        foreach (var targets in graph.Dependencies.Values)
        {
            foreach (var t in targets)
            {
                components.Add(t);
            }
        }

        return components;
    }

    private static Dictionary<string, int> ComputeFanIn(
        IReadOnlyDictionary<string, ISet<string>> dependencies)
    {
        var fanIn = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var targets in dependencies.Values)
        {
            foreach (var target in targets)
            {
                fanIn[target] = fanIn.GetValueOrDefault(target, 0) + 1;
            }
        }

        return fanIn;
    }

    private static Dictionary<string, double> ComputeAbstractnessByComponent(
        ArchConfig config, string projectPath)
    {
        var projectsByComponent = config.ComponentMap
            .GroupBy(kvp => kvp.Value, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Key).ToList(),
                StringComparer.Ordinal);

        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        if (!Directory.Exists(projectPath))
        {
            return result;
        }

        var csprojByName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(
                     projectPath, "*.csproj", SearchOption.AllDirectories))
        {
            if (IsBuildArtifactPath(path))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            // First wins when the scan root contains multiple same-named projects.
            csprojByName.TryAdd(name, Path.GetDirectoryName(path)!);
        }

        foreach (var (component, projects) in projectsByComponent)
        {
            var abstractCount = 0;
            var totalCount = 0;
            foreach (var project in projects)
            {
                if (!csprojByName.TryGetValue(project, out var dir))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (IsBuildArtifactPath(file))
                    {
                        continue;
                    }

                    CountTypesInFile(file, ref abstractCount, ref totalCount);
                }
            }

            result[component] = totalCount == 0 ? 0.0 : (double)abstractCount / totalCount;
        }

        return result;
    }

    /// <summary>
    /// Skip bin/obj trees under either Windows or Unix path separators.
    /// </summary>
    private static bool IsBuildArtifactPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains("/bin/", StringComparison.Ordinal)
        || path.Contains("/obj/", StringComparison.Ordinal);

    private static void CountTypesInFile(string file, ref int abstractCount, ref int totalCount)
    {
        var text = File.ReadAllText(file);
        var root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            totalCount++;
            if (IsAbstractType(type))
            {
                abstractCount++;
            }
        }
    }

    private static bool IsAbstractType(BaseTypeDeclarationSyntax type) =>
        type is InterfaceDeclarationSyntax
        || (type is TypeDeclarationSyntax td
            && td.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)));

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
