namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using System.Xml.Linq;

public static class CoverageReader
{
    public static IReadOnlySet<int> LoadCoverage(string coberturaPath)
    {
        var doc = XDocument.Load(coberturaPath);
        var covered = new HashSet<int>();

        foreach (var classElement in doc.Descendants("class"))
        {
            foreach (var lineElement in classElement.Elements("lines").Elements("line"))
            {
                var numberStr = lineElement.Attribute("number")?.Value;
                var hitsStr = lineElement.Attribute("hits")?.Value;

                if (numberStr is not null &&
                    int.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber) &&
                    hitsStr is not null &&
                    int.TryParse(hitsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hits) &&
                    hits > 0)
                {
                    covered.Add(lineNumber);
                }
            }
        }

        return covered;
    }

    public static (IReadOnlyList<MutationSite> Covered, IReadOnlyList<MutationSite> Uncovered)
        PartitionByCoverage(IReadOnlyList<MutationSite> sites, IReadOnlySet<int> coveredLines)
    {
        var covered = new List<MutationSite>();
        var uncovered = new List<MutationSite>();

        foreach (var site in sites)
        {
            // Roslyn returns 0-based line numbers; Cobertura XML uses 1-based
            var sourceLine = site.Line + 1;

            if (coveredLines.Contains(sourceLine))
            {
                covered.Add(site);
            }
            else
            {
                uncovered.Add(site);
            }
        }

        return (covered.AsReadOnly(), uncovered.AsReadOnly());
    }
}
