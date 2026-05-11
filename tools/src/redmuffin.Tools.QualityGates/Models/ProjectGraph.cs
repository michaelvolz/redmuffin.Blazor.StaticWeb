using System.Xml.Linq;

namespace redmuffin.Tools.QualityGates.Models;

public sealed record ProjectGraph(IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies)
{
    public static ProjectGraph From(string projectPath)
    {
        var csprojFiles = Directory.EnumerateFiles(
            projectPath, "*.csproj", SearchOption.AllDirectories)
            .Where(IsSourceProject);

        var dependencies = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var file in csprojFiles)
        {
            var projectName = System.IO.Path.GetFileNameWithoutExtension(file);
            var refs = new List<string>();

            var doc = XDocument.Load(file);
            foreach (var refElem in doc.Descendants("ProjectReference"))
            {
                var include = refElem.Attribute("Include")?.Value;
                if (include != null)
                {
                    var normalized = include.Replace('\\', System.IO.Path.DirectorySeparatorChar);
                    refs.Add(System.IO.Path.GetFileNameWithoutExtension(normalized));
                }
            }

            dependencies[projectName] = refs;
        }

        return new ProjectGraph(dependencies);
    }

    private static bool IsSourceProject(string path)
    {
        return !path.Contains("/bin/", StringComparison.Ordinal)
            && !path.Contains("/obj/", StringComparison.Ordinal)
            && !path.Contains("\\bin\\", StringComparison.Ordinal)
            && !path.Contains("\\obj\\", StringComparison.Ordinal);
    }
}
