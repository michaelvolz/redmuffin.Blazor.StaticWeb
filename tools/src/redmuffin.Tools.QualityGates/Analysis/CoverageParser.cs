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
            ProcessClassElement(classElement, result);
        }

        return result;
    }

    private static void ProcessClassElement(
        XElement classElement, IDictionary<(string FilePath, int LineNumber), int> result)
    {
        var filename = classElement.Attribute("filename")?.Value;
        if (filename is null)
        {
            return;
        }

        ProcessLines(classElement, filename, result);
    }

    private static void ProcessLines(
        XElement classElement, string filename,
        IDictionary<(string FilePath, int LineNumber), int> result)
    {
        foreach (var lineElement in classElement.Elements("lines").Elements("line"))
        {
            var lineNumberStr = lineElement.Attribute("number")?.Value;
            var hitsStr = lineElement.Attribute("hits")?.Value;

            if (TryParseLineNumber(lineNumberStr, out var lineNumber))
            {
                result[(filename, lineNumber)] = ParseHits(hitsStr);
            }
        }
    }

    public static bool TryParseLineNumber(string? str, out int number)
    {
        number = 0;
        return str is not null
            && int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    private static int ParseHits(string? hitsStr)
    {
        if (hitsStr is not null
            && int.TryParse(hitsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
        {
            return h;
        }

        return 0;
    }
}
