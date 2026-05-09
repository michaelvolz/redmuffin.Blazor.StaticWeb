using System.Xml.Linq;

namespace redmuffin.Tools.QualityGates.Models;

public sealed record ProjectGraph(Dictionary<string, List<string>> Dependencies)
{
    public static ProjectGraph From(string projectPath)
    {
        var csprojFiles = Directory.EnumerateFiles(
            projectPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/")
                && !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));

        var dependencies = new Dictionary<string, List<string>>();

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
                    refs.Add(System.IO.Path.GetFileNameWithoutExtension(include));
                }
            }

            dependencies[projectName] = refs;
        }

        return new ProjectGraph(dependencies);
    }
}
