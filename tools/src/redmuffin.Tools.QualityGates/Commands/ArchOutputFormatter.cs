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

        if (result.Metrics.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Component metrics (A abstractness, I instability, D distance):");
            foreach (var m in result.Metrics)
            {
                lines.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {m.Component}: A={m.Abstractness:F2} I={m.Instability:F2} D={m.Distance:F2} fan-in={m.FanIn} fan-out={m.FanOut} zone={m.Zone}"));
            }
        }

        return string.Join('\n', lines);
    }

    private static string FormatJson(ArchResult result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T12:25:33.8966161Z","moduleHash":"092c6739d9dc247223efa004742809f7639a6a872c6b62060cf8f010f4e37d69","forms":[{"id":"Format","line":14,"endLine":17,"hash":"4b0ec19e6f94af008fbc073adac55833ee9b6745734c94dc0fea03a4b6a38416"},{"id":"FormatText","line":19,"endLine":63,"hash":"28bb0511e82ef1d2a73e3f6b6c88ab7274f5dd38d754abce2571f9724c88c71e"},{"id":"FormatJson","line":65,"endLine":68,"hash":"51a6bf57c6823cb8212e6b4974dca332ce5633d14518224a537d641c585609fc"}]}
// clj-mutate-manifest-end
