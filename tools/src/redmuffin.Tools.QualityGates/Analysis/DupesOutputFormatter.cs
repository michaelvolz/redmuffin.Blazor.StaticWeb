namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using System.Text.Json;

/// <summary>
///     Formats duplicate detection results in text or JSON format,
///     matching the dry4clj output style.
/// </summary>
public static class DupesOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Format(IReadOnlyList<DupesCandidate> candidates, string format)
    {
        return format switch
        {
            "json" => FormatJson(candidates),
            _ => FormatText(candidates),
        };
    }

    private static string FormatText(IReadOnlyList<DupesCandidate> candidates)
    {
        if (candidates.Count == 0)
            return "No duplicate candidates found.";

        return FormatCandidateList(candidates);
    }

    private static string FormatCandidateList(IReadOnlyList<DupesCandidate> candidates)
    {
        var lines = new List<string>();
        foreach (var c in candidates)
        {
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"DUPLICATE score={c.Score:F2}"));
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"  {c.LeftFile}:{c.LeftStartLine}-{c.LeftEndLine}"));
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"  {c.RightFile}:{c.RightStartLine}-{c.RightEndLine}"));
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines).TrimEnd();
    }

    private static string FormatJson(IReadOnlyList<DupesCandidate> candidates)
    {
        var result = new { candidates };
        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
