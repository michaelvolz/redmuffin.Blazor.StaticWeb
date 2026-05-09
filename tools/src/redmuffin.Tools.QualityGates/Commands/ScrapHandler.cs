namespace redmuffin.Tools.QualityGates.Commands;

using System.Globalization;
using redmuffin.Tools.QualityGates.Analysis;

public static class ScrapHandler
{
    public static int Run(
        IReadOnlyList<FileScrapReport> reports,
        ScrapOptions options,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        if (options.Json)
        {
            return WriteJson(reports, output);
        }

        if (reports.Count == 0)
        {
            output.WriteLine("No test files found.");
            return 0;
        }

        var sorted = reports.OrderBy(r => r.FilePath, StringComparer.Ordinal).ToList();
        var worstExit = 0;

        foreach (var report in sorted)
        {
            var recommendation = ScrapRecommender.Decide(report);
            var status = recommendation.Mode is StabilityMode.Stable
                && recommendation.AiActionability is AiActionability.LeaveAlone
                ? "PASS" : "FAIL";

            if (status is "FAIL")
            {
                worstExit = 2;
            }

            // Default output: one line per file
            var worstExample = report.WorstExamples.Count > 0
                ? report.WorstExamples[0].ScrapScore.ToString("F1", CultureInfo.InvariantCulture)
                : "-";

            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{report.FilePath,-60} {recommendation.Mode,-8} {recommendation.AiActionability,-14} " +
                $"{status,-5} avg:{report.AvgScrap:F1} max:{report.MaxScrap:F1} worst:{worstExample}"));

            if (options.Verbose)
            {
                WritePerExampleTable(report, output);
            }
        }

        return worstExit;
    }

    private static void WritePerExampleTable(FileScrapReport report, TextWriter output)
    {
        output.WriteLine($"  {"Method",-40} {"Scrap",-8} {"Assert",-7} {"Setup",-6} {"Branches",-9} {"Smells"}");
        output.WriteLine(new string('-', 90));

        foreach (var m in report.Metrics.OrderByDescending(m => m.ScrapScore))
        {
            var smells = string.Join(", ", m.SmellLabels.Select(s => s.ToString()));
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {m.Method.MethodName,-40} {m.ScrapScore,-8:F1} {m.AssertionCount,-7} " +
                $"{m.SetupDepth,-6} {m.BranchCount,-9} {smells}"));
        }
    }

    private static int WriteJson(IReadOnlyList<FileScrapReport> reports, TextWriter output)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(reports, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        });
        output.WriteLine(json);
        return HasAnyFailure(reports) ? 2 : 0;
    }

    private static bool HasAnyFailure(IReadOnlyList<FileScrapReport> reports)
    {
        foreach (var report in reports)
        {
            var recommendation = ScrapRecommender.Decide(report);
            if (recommendation.Mode is not StabilityMode.Stable
                || recommendation.AiActionability is not AiActionability.LeaveAlone)
            {
                return true;
            }
        }

        return false;
    }
}
