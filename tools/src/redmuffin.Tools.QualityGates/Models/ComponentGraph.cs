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
            var component = config.ComponentMap.GetValueOrDefault(project, "Default");

            if (ignored.Contains(component))
            {
                continue;
            }

            if (string.Equals(component, "Default", StringComparison.Ordinal))
            {
                unmapped.Add(project);
            }

            foreach (var targetRef in refs)
            {
                var targetComponent = config.ComponentMap.GetValueOrDefault(targetRef, "Default");
                if (ignored.Contains(targetComponent))
                {
                    continue;
                }

                if (string.Equals(targetComponent, component, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!deps.TryGetValue(component, out var value))
                {
                    value = new HashSet<string>(StringComparer.Ordinal);
                    deps[component] = value;
                }

                if (!deps.ContainsKey(targetComponent))
                {
                    deps[targetComponent] = new HashSet<string>(StringComparer.Ordinal);
                }

                value.Add(targetComponent);
            }
        }

        return new ComponentGraph(deps, unmapped);
    }
}
