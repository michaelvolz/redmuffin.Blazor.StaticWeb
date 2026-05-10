using System.Globalization;
using redmuffin.Tools.QualityGates.Models;

namespace redmuffin.Tools.QualityGates.Commands;

public static class ArchOutputFormatter
{
    public static string Format(ArchResult result, bool json)
    {
        if (json)
        {
            return FormatJson(result);
        }

        return FormatText(result);
    }

    private static string FormatText(ArchResult result)
    {
        var lines = new List<string>
        {
            "Architecture Gate Results",
            "-------------------------",
            string.Create(CultureInfo.InvariantCulture, $"{result.ProjectsScanned} projects scanned, ") +
                string.Create(CultureInfo.InvariantCulture, $"{result.ComponentsDefined} components defined"),
            $"{result.Violations.Count} violations found, " +
                $"{result.Cycles.Count} cycles found",
        };

        if (result.Violations.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Violations:");
            foreach (var v in result.Violations)
            {
                lines.Add($"  {v.SourceComponent} → {v.TargetComponent}");
                lines.Add($"    Reason: {v.Reason}");
            }
        }

        if (result.Cycles.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Cycles:");
            foreach (var c in result.Cycles)
            {
                lines.Add($"  {string.Join(" → ", c.Components)}");
            }
        }

        return string.Join('\n', lines);
    }

    private static string FormatJson(ArchResult result)
    {
        var exitCode = result.ExitCode.ToString(CultureInfo.InvariantCulture);
        var projectsScanned = result.ProjectsScanned.ToString(CultureInfo.InvariantCulture);
        var componentsDefined = result.ComponentsDefined.ToString(CultureInfo.InvariantCulture);
        var jsonText = $$"""
            {
              "exitCode": {{exitCode}},
              "projectsScanned": {{projectsScanned}},
              "componentsDefined": {{componentsDefined}},
              "violations": {{FormatJsonViolations(result)}},
              "cycles": {{FormatJsonCycles(result)}}
            }
            """;
        return jsonText;
    }

    private static string FormatJsonViolations(ArchResult result)
    {
        if (result.Violations.Count == 0)
        {
            return "[]";
        }

        var items = result.Violations.Select(v =>
            $$"""
            {
              "sourceProject": "{{Escape(v.SourceProject)}}",
              "targetProject": "{{Escape(v.TargetProject)}}",
              "sourceComponent": "{{Escape(v.SourceComponent)}}",
              "targetComponent": "{{Escape(v.TargetComponent)}}",
              "reason": "{{Escape(v.Reason)}}"
            }
            """);

        return "[\n" + string.Join(",\n", items) + "\n    ]";
    }

    private static string FormatJsonCycles(ArchResult result)
    {
        if (result.Cycles.Count == 0)
        {
            return "[]";
        }

        var items = result.Cycles.Select(c =>
        {
            var comps = string.Join("\", \"", c.Components);
            return $$"""
            {
              "components": ["{{comps}}"],
              "length": {{c.Length}}
            }
            """;
        });

        return "[\n" + string.Join(",\n", items) + "\n    ]";
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
