namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using System.Text;
using System.Text.Json;

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

        var sb = new StringBuilder();
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            sb.Append("DUPLICATE score=");
            sb.Append(c.Score.ToString("F2", CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.Append("  ");
            sb.Append(c.LeftFile);
            sb.Append(':');
            sb.Append(c.LeftStartLine.ToString(CultureInfo.InvariantCulture));
            sb.Append('-');
            sb.Append(c.LeftEndLine.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.Append("  ");
            sb.Append(c.RightFile);
            sb.Append(':');
            sb.Append(c.RightStartLine.ToString(CultureInfo.InvariantCulture));
            sb.Append('-');
            sb.Append(c.RightEndLine.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine();

            if (i < candidates.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string FormatJson(IReadOnlyList<DupesCandidate> candidates)
    {
        var result = new { candidates };
        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
