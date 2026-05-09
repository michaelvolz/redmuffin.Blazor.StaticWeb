namespace redmuffin.Tools.QualityGates.Models;

public sealed record ComponentGraph(
    Dictionary<string, HashSet<string>> Dependencies,
    HashSet<string> UnmappedProjects)
{
    public static ComponentGraph From(ProjectGraph projects, ArchConfig config)
    {
        var deps = new Dictionary<string, HashSet<string>>();
        var unmapped = new HashSet<string>();

        foreach (var (project, refs) in projects.Dependencies)
        {
            var component = config.ComponentMap.GetValueOrDefault(project, "Default");

            if (config.IgnoredComponents.Contains(component))
            {
                continue;
            }

            if (component == "Default")
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

                if (targetComponent == component)
                {
                    continue;
                }

                if (!deps.ContainsKey(component))
                {
                    deps[component] = [];
                }

                if (!deps.ContainsKey(targetComponent))
                {
                    deps[targetComponent] = [];
                }

                deps[component].Add(targetComponent);
            }
        }

        return new ComponentGraph(deps, unmapped);
    }
}
