using System.Xml.Linq;

namespace redmuffin.Tools.QualityGates.Models;

public sealed record ProjectGraph(Dictionary<string, List<string>> Dependencies)
{
    public static ProjectGraph From(string projectPath)
    {
        var csprojFiles = Directory.EnumerateFiles(
            projectPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/", StringComparison.Ordinal) && !f.Contains("/obj/", StringComparison.Ordinal)
                && !f.Contains("\\bin\\", StringComparison.Ordinal) && !f.Contains("\\obj\\", StringComparison.Ordinal));

        var dependencies = new Dictionary<string, List<string>>(StringComparer.Ordinal);

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
}
