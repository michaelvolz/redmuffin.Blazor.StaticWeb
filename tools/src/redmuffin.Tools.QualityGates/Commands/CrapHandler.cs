namespace redmuffin.Tools.QualityGates.Commands;

using System.Globalization;
using redmuffin.Tools.QualityGates.Analysis;

public static class CrapHandler
{
    public static int Run(
        IReadOnlyList<MethodCrap> results,
        int maxCrap = 8,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        var sorted = results.OrderByDescending(r => r.CrapScore).ToList();

        WriteTable(sorted, output);

        return sorted.Exists(r => r.CrapScore > maxCrap && !r.IsCoverageGap) ? 2 : 0;
    }

    private static void WriteTable(List<MethodCrap> results, TextWriter output)
    {
        if (results.Count == 0)
        {
            output.WriteLine("No methods found.");
            return;
        }

        output.WriteLine($"{"CRAP",-8} {"CC",-4} {"Coverage",-10} {"Status",-14} {"Method",-30} {"File:Line"}");

        foreach (var r in results)
        {
            var coverage = r.Coverage.ToString("P0", CultureInfo.InvariantCulture);
            var status = r.IsCoverageGap ? "COVERAGE GAP" : (r.CrapScore > 8 ? "FAIL" : "PASS");
            var location = string.Create(CultureInfo.InvariantCulture, $"{r.FilePath}:{r.StartLine}");
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{r.CrapScore,8:F1} {r.Complexity,-4} {coverage,-10} {status,-14} {r.MethodName,-30} {location}"));
        }
    }
}
