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

        return sorted.Any(r => r.CrapScore > maxCrap) ? 2 : 0;
    }

    private static void WriteTable(List<MethodCrap> results, TextWriter output)
    {
        if (results.Count == 0)
        {
            output.WriteLine("No methods found.");
            return;
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{"CRAP",-8} {"CC",-4} {"Coverage",-10} {"Method",-30} {"File:Line"}"));

        foreach (var r in results)
        {
            var coverage = r.Coverage.ToString("P0", CultureInfo.InvariantCulture);
            var location = FormattableString.Invariant($"{r.FilePath}:{r.StartLine}");
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{r.CrapScore,8:F1} {r.Complexity,-4} {coverage,-10} {r.MethodName,-30} {location}"));
        }
    }
}
