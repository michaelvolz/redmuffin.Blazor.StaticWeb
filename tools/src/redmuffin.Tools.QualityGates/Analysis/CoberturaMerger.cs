namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using System.Xml;
using System.Xml.Linq;

public static class CoberturaMerger
{
    public static void Merge(IReadOnlyList<string> inputPaths, string outputPath)
    {
        if (inputPaths.Count == 0)
            throw new ArgumentException("At least one input coverage file is required.", nameof(inputPaths));

        if (inputPaths.Count == 1)
        {
            File.Copy(inputPaths[0], outputPath, overwrite: true);
            return;
        }

        var classLines = LoadAllClassLines(inputPaths);
        WriteMergedDocument(classLines, outputPath);
    }

    public static IReadOnlyDictionary<string, Dictionary<int, int>> LoadAllClassLines(
        IReadOnlyList<string> inputPaths)
    {
        var classMap = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);

        foreach (var path in inputPaths)
        {
            var doc = XDocument.Load(path);
            foreach (var classElement in doc.Descendants("class"))
            {
                AddClassLines(classElement, classMap);
            }
        }

        return classMap;
    }

    private static void AddClassLines(
        XElement classElement,
        Dictionary<string, Dictionary<int, int>> classMap)
    {
        var key = GetClassKey(classElement);

        if (key.Length == 0)
        {
            return;
        }

        if (!classMap.TryGetValue(key, out var lineMap))
        {
            lineMap = new Dictionary<int, int>();
            classMap[key] = lineMap;
        }

        foreach (var lineElement in classElement.Descendants("line"))
        {
            AggregateLineHit(lineElement, lineMap);
        }
    }

    private static string GetClassKey(XElement classElement)
    {
        var filename = classElement.Attribute("filename")?.Value;
        var className = classElement.Attribute("name")?.Value;
        return filename is { Length: > 0 } ? filename
            : className is { Length: > 0 } ? className
            : string.Empty;
    }

    private static void AggregateLineHit(XElement lineElement, Dictionary<int, int> lineMap)
    {
        var lineNumberStr = lineElement.Attribute("number")?.Value;
        var hitsStr = lineElement.Attribute("hits")?.Value;

        if (lineNumberStr is not null
            && int.TryParse(lineNumberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber))
        {
            var hits = int.TryParse(hitsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ? h : 0;
            lineMap.TryGetValue(lineNumber, out var existing);
            lineMap[lineNumber] = existing + hits;
        }
    }

    private static void WriteMergedDocument(
        IReadOnlyDictionary<string, Dictionary<int, int>> classMap, string outputPath)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
        };

        using var writer = XmlWriter.Create(outputPath, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("coverage");
        writer.WriteAttributeString("line-rate", "0");
        writer.WriteAttributeString("branch-rate", "0");
        writer.WriteAttributeString("version", "1.9");
        writer.WriteStartElement("packages");
        writer.WriteStartElement("package");
        writer.WriteStartElement("classes");

        foreach (var (filename, lineMap) in classMap.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            writer.WriteStartElement("class");
            writer.WriteAttributeString("name", filename);
            writer.WriteAttributeString("filename", filename);
            writer.WriteAttributeString("line-rate", "0");
            writer.WriteAttributeString("branch-rate", "0");
            writer.WriteStartElement("lines");

            foreach (var (lineNumber, hits) in lineMap.OrderBy(kvp => kvp.Key))
            {
                writer.WriteStartElement("line");
                writer.WriteAttributeString("number", lineNumber.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("hits", hits.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("branch", "false");
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // lines
            writer.WriteEndElement(); // class
        }

        writer.WriteEndElement(); // classes
        writer.WriteEndElement(); // package
        writer.WriteEndElement(); // packages
        writer.WriteEndElement(); // coverage
        writer.WriteEndDocument();
    }
}
