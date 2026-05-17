namespace redmuffin.Tools.QualityGates.Commands;

using System.Globalization;
using redmuffin.Tools.QualityGates.Analysis;

public static class DepthHandler
{
    public static int Run(
        IReadOnlyList<DepthResult> results,
        int failThreshold = 3,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        var sorted = results.OrderByDescending(r => r.CompositeScore).ToList();

        WriteTable(sorted, output, failThreshold);

        return sorted.Any(r => r.CompositeScore >= failThreshold) ? 2 : 0;
    }

    private static void WriteTable(
        List<DepthResult> results,
        TextWriter output,
        int failThreshold)
    {
        if (results.Count == 0)
        {
            output.WriteLine("No methods with structural depth issues found.");
            return;
        }

        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{"Status",-6} {"File:Line",-35} {"Method",-25} {"Score",-14} Signals"));

        foreach (var r in results)
        {
            var severity = r.CompositeScore >= failThreshold ? "FAIL"
                : r.CompositeScore == 2 ? "WARN"
                : "INFO";
            var location = string.Create(
                CultureInfo.InvariantCulture,
                $"{r.FilePath}:{r.LineNumber}");
            var signals = string.Join(" + ", r.Signals);
            var composite = string.Create(
                CultureInfo.InvariantCulture,
                $"composite={r.CompositeScore}");

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{severity,-6} {location,-35} {r.MethodName + "()",-25} {composite,-14} [{signals}]"));
        }
    }
}
