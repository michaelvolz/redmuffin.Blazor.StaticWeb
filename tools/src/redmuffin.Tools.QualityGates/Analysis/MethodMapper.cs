namespace redmuffin.Tools.QualityGates.Analysis;

public static class MethodMapper
{
    public static IReadOnlyList<MethodCrap> Map(
        IReadOnlyList<MethodComplexity> methods,
        IDictionary<(string FilePath, int LineNumber), int> coverage)
    {
        return methods.Select(m => ComputeCrap(m, coverage)).ToList().AsReadOnly();
    }

    private static MethodCrap ComputeCrap(
        MethodComplexity method,
        IDictionary<(string FilePath, int LineNumber), int> coverage)
    {
        var totalLines = method.EndLine - method.StartLine + 1;

        if (totalLines <= 0)
        {
            var crap = (double)(method.Complexity * method.Complexity) + method.Complexity;
            return new MethodCrap(method.MethodName, method.FilePath, method.StartLine, method.Complexity, 0.0, crap);
        }

        var coveredLines = 0;
        for (var line = method.StartLine; line <= method.EndLine; line++)
        {
            if (coverage.TryGetValue((method.FilePath, line), out var hits) && hits > 0)
            {
                coveredLines++;
            }
        }

        var coverageRatio = (double)coveredLines / totalLines;
        var crapScore = ComputeCrapScore(method.Complexity, coverageRatio);

        return new MethodCrap(method.MethodName, method.FilePath, method.StartLine, method.Complexity, coverageRatio, crapScore);
    }

    private static double ComputeCrapScore(int complexity, double coverage)
    {
        var uncoveredRatio = 1.0 - coverage;
        return (complexity * complexity * uncoveredRatio * uncoveredRatio * uncoveredRatio) + complexity;
    }
}
