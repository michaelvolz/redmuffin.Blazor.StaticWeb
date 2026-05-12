namespace redmuffin.Tools.QualityGates.Analysis;

using System.Xml.Linq;

/// <summary>
///     Discovers source and test projects from a .slnx solution file.
///     Walks up from the starting directory to find the nearest .slnx,
///     parses its XML to extract project paths, and classifies each
///     as source or test by checking for IsTestProject in the .csproj.
/// </summary>
public static class SlnxProjectDiscovery
{
    public static SlnxDiscoveredProjects Discover(string? startDirectory)
    {
        var slnxPath = startDirectory is not null
            ? FindNearestSlnx(startDirectory)
            : FindNearestSlnx(Environment.CurrentDirectory);
        return DiscoverFromSlnx(slnxPath);
    }

    public static SlnxDiscoveredProjects DiscoverFromSlnx(string slnxPath)
    {
        slnxPath = Path.GetFullPath(slnxPath);

        if (!File.Exists(slnxPath))
            throw new FileNotFoundException(
                $"Solution file not found: {slnxPath}");

        var slnxDirectory = Path.GetDirectoryName(slnxPath)!;

        var sourceProjects = new List<string>();
        var testProjects = new List<string>();

        var doc = XDocument.Load(slnxPath);
        var projectElements = doc.Descendants("Project");

        foreach (var projectElement in projectElements)
        {
            var relativePath = projectElement.Attribute("Path")?.Value;
            if (string.IsNullOrWhiteSpace(relativePath)) continue;

            var absolutePath = Path.GetFullPath(
                Path.Combine(slnxDirectory, relativePath));

            if (!File.Exists(absolutePath)) continue;

            if (IsTestProject(absolutePath))
                testProjects.Add(absolutePath);
            else
                sourceProjects.Add(absolutePath);
        }

        return new SlnxDiscoveredProjects(
            sourceProjects,
            testProjects,
            slnxPath
        );
    }

    private static string FindNearestSlnx(string startDirectory)
    {
        startDirectory = Path.GetFullPath(startDirectory);

        if (!Directory.Exists(startDirectory))
            throw new DirectoryNotFoundException(
                $"Directory not found: {startDirectory}");

        var current = startDirectory;

        while (current is not null)
        {
            var slnxFiles = Directory.GetFiles(current, "*.slnx");
            if (slnxFiles.Length > 0)
                return slnxFiles[0];

            current = Path.GetDirectoryName(current);
        }

        throw new FileNotFoundException(
            "No .slnx file found walking up from " + startDirectory);
    }

    private static bool IsTestProject(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var propertyGroup = doc.Descendants("PropertyGroup").FirstOrDefault();
        if (propertyGroup is null) return false;

        var isTestProject = propertyGroup
            .Elements("IsTestProject")
            .FirstOrDefault();

        return isTestProject is not null &&
               string.Equals(isTestProject.Value, "true",
                   StringComparison.OrdinalIgnoreCase);
    }
}
