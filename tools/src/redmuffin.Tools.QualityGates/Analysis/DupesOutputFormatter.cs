namespace redmuffin.Tools.QualityGates.Analysis;

using System.Text.Json;

/// <summary>
///     Formats duplicate detection results in text or JSON format,
///     matching the dry4clj output style.
/// </summary>
public static class DupesOutputFormatter
{
    public static string Format(List<DupesCandidate> candidates, string format)
    {
        return format switch
        {
            "json" => FormatJson(candidates),
            _ => FormatText(candidates)
        };
    }

    private static string FormatText(List<DupesCandidate> candidates)
    {
        if (candidates.Count == 0)
            return "No duplicate candidates found.";

        var lines = new List<string>();
        foreach (var c in candidates)
        {
            lines.Add($"DUPLICATE score={c.Score:F2}");
            lines.Add($"  {c.LeftFile}:{c.LeftStartLine}-{c.LeftEndLine}");
            lines.Add($"  {c.RightFile}:{c.RightStartLine}-{c.RightEndLine}");
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines).TrimEnd();
    }

    private static string FormatJson(List<DupesCandidate> candidates)
    {
        var result = new { candidates };
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
