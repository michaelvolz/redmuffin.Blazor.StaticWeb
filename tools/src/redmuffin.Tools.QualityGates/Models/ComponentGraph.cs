namespace redmuffin.Tools.QualityGates.Models;

public sealed record ComponentGraph(
    Dictionary<string, HashSet<string>> Dependencies,
    HashSet<string> UnmappedProjects)
{
    public static ComponentGraph From(ProjectGraph projects, ArchConfig config)
    {
        var deps = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var unmapped = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (project, refs) in projects.Dependencies)
        {
            var component = config.ComponentMap.GetValueOrDefault(project, "Default");

            if (config.IgnoredComponents.Contains(component))
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
                if (config.IgnoredComponents.Contains(targetComponent))
                {
                    continue;
                }

                if (string.Equals(targetComponent, component, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!deps.TryGetValue(component, out var value))
                {
                    value = [];
                    deps[component] = value;
                }

                if (!deps.ContainsKey(targetComponent))
                {
                    deps[targetComponent] = [];
                }

                value.Add(targetComponent);
            }
        }

        return new ComponentGraph(deps, unmapped);
    }
}
