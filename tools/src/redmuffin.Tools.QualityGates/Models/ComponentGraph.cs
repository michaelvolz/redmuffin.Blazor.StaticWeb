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
        var builder = new GraphBuilder(config, ignored, deps, unmapped);

        foreach (var (project, refs) in projects.Dependencies)
        {
            AddProjectDependencies(project, refs, builder);
        }

        return new ComponentGraph(deps, unmapped);
    }

    private static void AddProjectDependencies(string project, IReadOnlyList<string> refs, GraphBuilder b)
    {
        var component = b.Config.ComponentMap.GetValueOrDefault(project, "Default");
        if (b.Ignored.Contains(component)) return;

        if (string.Equals(component, "Default", StringComparison.Ordinal))
        {
            b.Unmapped.Add(project);
        }

        foreach (var targetRef in refs)
        {
            AddTargetDependency(component, targetRef, b);
        }
    }

    private static void AddTargetDependency(string component, string targetRef, GraphBuilder b)
    {
        var targetComponent = b.Config.ComponentMap.GetValueOrDefault(targetRef, "Default");
        if (b.Ignored.Contains(targetComponent)) return;
        if (string.Equals(targetComponent, component, StringComparison.Ordinal)) return;

        EnsureComponentEntry(b.Deps, component);
        EnsureComponentEntry(b.Deps, targetComponent);
        b.Deps[component].Add(targetComponent);
    }

    private static void EnsureComponentEntry(Dictionary<string, ISet<string>> deps, string key)
    {
        if (!deps.ContainsKey(key))
        {
            deps[key] = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private sealed record GraphBuilder(
        ArchConfig Config,
        HashSet<string> Ignored,
        Dictionary<string, ISet<string>> Deps,
        HashSet<string> Unmapped);
}
