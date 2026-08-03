namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
/// SCRAP duplication channel policy: form stats, complexity metrics,
/// Harmful / CaseMatrix / Subject classification, and channel routing.
/// </summary>
public static class DuplicationChannelClassifier
{
    public static int ComputeSharedForms(
        IReadOnlyList<int> indices,
        IReadOnlyList<IReadOnlyList<string>> normalized)
    {
        if (indices.Count == 0)
        {
            return 0;
        }

        var firstSet = new HashSet<string>(normalized[indices[0]], StringComparer.Ordinal);
        for (var i = 1; i < indices.Count; i++)
        {
            firstSet.IntersectWith(normalized[indices[i]]);
        }

        return firstSet.Count;
    }

    public static int ComputeVariablePoints(
        IReadOnlyList<int> indices,
        IReadOnlyList<IReadOnlyList<string>> normalized)
    {
        if (indices.Count <= 1)
        {
            return 0;
        }

        var union = new HashSet<string>(normalized[indices[0]], StringComparer.Ordinal);
        for (var i = 1; i < indices.Count; i++)
        {
            union.UnionWith(normalized[indices[i]]);
        }

        var intersection = new HashSet<string>(normalized[indices[0]], StringComparer.Ordinal);
        for (var i = 1; i < indices.Count; i++)
        {
            intersection.IntersectWith(normalized[indices[i]]);
        }

        return union.Count - intersection.Count;
    }

    public static SimpleMethodMetrics ComputeSimpleMetrics(TestMethod method)
    {
        var lineCount = method.EndLine - method.StartLine + 1;
        return new SimpleMethodMetrics(
            lineCount,
            TestMethodMetricsCalculator.CountAssertions(method.BodySyntax),
            TestMethodMetricsCalculator.CountBranches(method.BodySyntax),
            TestMethodMetricsCalculator.ComputeSetupDepth(method.BodySyntax));
    }

    public static ChannelType ClassifyChannel(
        IReadOnlyList<TestMethod> methods,
        int sharedForms,
        int variablePoints,
        IReadOnlyList<SimpleMethodMetrics> metrics)
    {
        if (sharedForms >= 3 && variablePoints <= 4)
        {
            return ChannelType.Harmful;
        }

        if (AllLowComplexity(metrics))
        {
            return ChannelType.CaseMatrix;
        }

        return ChannelType.Subject;
    }

    public static bool AllLowComplexity(IReadOnlyList<SimpleMethodMetrics> metrics)
    {
        return metrics.All(m =>
            m.LineCount <= 12
            && m.AssertionCount <= 1
            && m.BranchCount <= 0
            && m.SetupDepth <= 2
            && TestMethodMetricsCalculator.ComputeComplexityScore(m.BranchCount + 1) <= 18);
    }

    public static void RouteToChannel(
        ChannelType channel,
        DuplicationChannel dupChannel,
        ICollection<DuplicationChannel> allHarmful,
        ICollection<DuplicationChannel> allCaseMatrix,
        ICollection<DuplicationChannel> allSubject)
    {
        switch (channel)
        {
            case ChannelType.Harmful:
                allHarmful.Add(dupChannel);
                break;
            case ChannelType.CaseMatrix:
                allCaseMatrix.Add(dupChannel);
                break;
            case ChannelType.Subject:
                allSubject.Add(dupChannel);
                break;
        }
    }

    public sealed record SimpleMethodMetrics(
        int LineCount,
        int AssertionCount,
        int BranchCount,
        int SetupDepth);
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T14:06:00.2829753Z","moduleHash":"6cfc33f6c539480f854c48468470cf17712389af9fc901e031702b600f7b5d9a","forms":[{"id":"ComputeSharedForms","line":8,"endLine":24,"hash":"3a3b345d17b4230fea95dd24d1709cf80f711b88e35351f22b0d35be1cab6520"},{"id":"ComputeVariablePoints","line":26,"endLine":48,"hash":"7320449248e7f7d007c775aa3027c59f738ff5a5ba16bf4f3a4c7595f99fb33d"},{"id":"ComputeSimpleMetrics","line":50,"endLine":58,"hash":"6005599add86adc6f71457dabee77458afbba116595cae3737ab2fc794c007f5"},{"id":"ClassifyChannel","line":60,"endLine":77,"hash":"c8cc25197826f86a2ca81b27949477d061c6f8152a2c67d08307d839a58df0d4"},{"id":"AllLowComplexity","line":79,"endLine":87,"hash":"822ef40b70b890b2674c636726f907cf570f1fac68ed5be48fb566513fb8590f"},{"id":"RouteToChannel","line":89,"endLine":108,"hash":"db283ff2b4727b1e485e53950ee94fdf5b5be7d7cd8f73eb6c9612dd5a4546f5"}]}
// clj-mutate-manifest-end
