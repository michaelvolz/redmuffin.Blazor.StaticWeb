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
                if (TryParseLine(lineElement, out var lineNumber))
                {
                    covered.Add(lineNumber);
                }
            }
        }

        return covered;
    }

    private static bool TryParseLine(XElement lineElement, out int lineNumber)
    {
        if (!TryParseAttributes(lineElement, out var numberStr, out var hitsStr))
        {
            lineNumber = 0;
            return false;
        }

        return int.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber)
            && int.TryParse(hitsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hits)
            && hits > 0;
    }

    private static bool TryParseAttributes(XElement lineElement, out string? numberStr, out string? hitsStr)
    {
        numberStr = lineElement.Attribute("number")?.Value;
        hitsStr = lineElement.Attribute("hits")?.Value;
        return numberStr is not null && hitsStr is not null;
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
