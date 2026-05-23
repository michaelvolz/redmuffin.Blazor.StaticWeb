using System.Globalization;
using System.Text.Json;
using redmuffin.Tools.QualityGates.Models;

namespace redmuffin.Tools.QualityGates.Commands;

public static class ArchOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Format(ArchResult result, bool json)
    {
        return json ? FormatJson(result) : FormatText(result);
    }

    private static string FormatText(ArchResult result)
    {
        var lines = new List<string>
        {
            "Architecture Gate Results",
            "-------------------------",
            $"{result.ProjectsScanned.ToString(CultureInfo.InvariantCulture)} projects scanned, {result.ComponentsDefined.ToString(CultureInfo.InvariantCulture)} components defined",
            $"{result.Violations.Count} violations found, {result.Cycles.Count} cycles found",
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
        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
