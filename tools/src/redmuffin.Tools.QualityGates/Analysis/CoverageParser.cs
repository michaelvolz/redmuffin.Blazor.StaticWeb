namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using System.Xml.Linq;

public static class CoverageParser
{
    public static IDictionary<(string FilePath, int LineNumber), int> Parse(string coverageFilePath)
    {
        var doc = XDocument.Load(coverageFilePath);
        var result = new Dictionary<(string FilePath, int LineNumber), int>();

        foreach (var classElement in doc.Descendants("class"))
        {
            var filename = classElement.Attribute("filename")?.Value;
            if (filename is null)
            {
                continue;
            }

            foreach (var lineElement in classElement.Elements("lines").Elements("line"))
            {
                var lineNumberStr = lineElement.Attribute("number")?.Value;
                var hitsStr = lineElement.Attribute("hits")?.Value;

                if (lineNumberStr is not null && int.TryParse(lineNumberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber))
                {
                    var hits = hitsStr is not null && int.TryParse(hitsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ? h : 0;
                    result[(filename, lineNumber)] = hits;
                }
            }
        }

        return result;
    }
}
