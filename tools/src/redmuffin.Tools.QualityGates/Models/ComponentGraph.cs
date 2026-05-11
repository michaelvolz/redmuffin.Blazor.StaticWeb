namespace redmuffin.Tools.QualityGates.Models;

public sealed record ComponentGraph(
    IReadOnlyDictionary<string, ISet<string>> Dependencies,
    IReadOnlySet<string> UnmappedProjects)
{
    public static ComponentGraph From(ProjectGraph projects, ArchConfig config)
    {
        var deps = new Dictionary<string, ISet<string>>(StringComparer.Ordinal);
        var unmapped = new HashSet<string>(StringComparer.Ordinal);
        var ignored = new HashSet<string>(config.IgnoredComponents, StringComparer.Ordinal);

        foreach (var (project, refs) in projects.Dependencies)
        {
            AddProjectDependencies(project, refs, config, ignored, deps, unmapped);
        }

        return new ComponentGraph(deps, unmapped);
    }

    private static void AddProjectDependencies(
        string project, IReadOnlyList<string> refs,
        ArchConfig config, HashSet<string> ignored,
        Dictionary<string, ISet<string>> deps, HashSet<string> unmapped)
    {
        var component = config.ComponentMap.GetValueOrDefault(project, "Default");
        if (ignored.Contains(component)) return;

        if (string.Equals(component, "Default", StringComparison.Ordinal))
        {
            unmapped.Add(project);
        }

        foreach (var targetRef in refs)
        {
            AddTargetDependency(component, targetRef, config, ignored, deps);
        }
    }

    private static void AddTargetDependency(
        string component, string targetRef,
        ArchConfig config, HashSet<string> ignored,
        Dictionary<string, ISet<string>> deps)
    {
        var targetComponent = config.ComponentMap.GetValueOrDefault(targetRef, "Default");
        if (ignored.Contains(targetComponent)) return;
        if (string.Equals(targetComponent, component, StringComparison.Ordinal)) return;

        EnsureComponentEntry(deps, component);
        EnsureComponentEntry(deps, targetComponent);
        deps[component].Add(targetComponent);
    }

    private static void EnsureComponentEntry(Dictionary<string, ISet<string>> deps, string key)
    {
        if (!deps.ContainsKey(key))
        {
            deps[key] = new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
