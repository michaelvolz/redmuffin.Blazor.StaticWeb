#pragma warning disable CA1859 // Conflicts with MA0016: collection abstractions preferred over concrete types (see rm-guide-warnings §Known Conflicts)

namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class ScrapDuplication
{
    private const double JaccardThreshold = 0.5;

    public static double JaccardSimilarity(ISet<string> setA, ISet<string> setB)
    {
        if (setA.Count == 0 && setB.Count == 0)
        {
            return 0.0;
        }

        var intersectionCount = 0;
        var unionCount = setB.Count;

        foreach (var item in setA)
        {
            if (setB.Contains(item))
            {
                intersectionCount++;
            }
            else
            {
                unionCount++;
            }
        }

        return unionCount == 0 ? 0.0 : (double)intersectionCount / unionCount;
    }

    public static DuplicationResults Analyze(IReadOnlyList<TestMethod> methods)
    {
        if (methods.Count == 0)
        {
            return new DuplicationResults([], [], [], 0.0);
        }

        var byFile = methods.GroupBy(m => m.FilePath, StringComparer.Ordinal).ToList();
        var allHarmful = new List<DuplicationChannel>();
        var allCaseMatrix = new List<DuplicationChannel>();
        var allSubject = new List<DuplicationChannel>();
        var clusterId = 0;

        foreach (var fileGroup in byFile)
        {
            var fileMethods = fileGroup.ToList();
            if (fileMethods.Count < 2)
            {
                continue;
            }

            var normalized = NormalizeFileMethods(fileMethods);
            var featureSets = normalized.Select(f => new HashSet<string>(f, StringComparer.Ordinal)).ToList();
            var edges = BuildJaccardEdges(fileMethods.Count, featureSets);
            var parent = InitUnionFind(fileMethods.Count);
            foreach (var (a, b) in edges)
            {
                Union(parent, a, b);
            }

            var clusters = GroupByRoot(parent, fileMethods.Count);
            var clusteredIndices = new HashSet<int>();

            var ctx = new DuplicationContext(
                fileMethods, normalized, clusteredIndices,
                allHarmful, allCaseMatrix, allSubject);

            clusterId = ClassifyAndCollectClusters(clusters, ctx, clusterId);

            CollectSubjectRepetition(fileMethods, normalized, clusteredIndices, allSubject, ref clusterId);
        }

        return new DuplicationResults(
            HarmfulDuplication: allHarmful,
            CaseMatrixRepetition: allCaseMatrix,
            SubjectRepetition: allSubject,
            EffectiveDuplicationScore: 0.0);
    }

    private static List<IReadOnlyList<string>> NormalizeFileMethods(IReadOnlyList<TestMethod> fileMethods)
    {
        return fileMethods.Select(m =>
        {
            var methodDecl = m.BodySyntax as MethodDeclarationSyntax
                ?? m.BodySyntax.AncestorsAndSelf()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();

            return methodDecl is not null
                ? TestNormalizer.Normalize(methodDecl)
                : (IReadOnlyList<string>)[];
        }).ToList();
    }

    private static List<(int A, int B)> BuildJaccardEdges(
        int n, List<HashSet<string>> featureSets)
    {
        var edges = new List<(int A, int B)>();
        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                if (JaccardSimilarity(featureSets[i], featureSets[j]) >= JaccardThreshold)
                    edges.Add((i, j));
            }
        }

        return edges;
    }

    private static int[] InitUnionFind(int n)
    {
        var parent = new int[n];
        for (var i = 0; i < n; i++)
            parent[i] = i;
        return parent;
    }

    private static Dictionary<int, List<int>> GroupByRoot(int[] parent, int n)
    {
        var clusters = new Dictionary<int, List<int>>();
        for (var i = 0; i < n; i++)
        {
            var root = Find(parent, i);
            if (!clusters.TryGetValue(root, out var value))
            {
                value = [];
                clusters[root] = value;
            }

            value.Add(i);
        }

        return clusters;
    }

    private static int ClassifyAndCollectClusters(
        Dictionary<int, List<int>> clusters,
        DuplicationContext ctx,
        int clusterId)
    {
        foreach (var kvp in clusters)
        {
            var indices = kvp.Value;
            if (indices.Count < 2) continue;
            clusterId = ProcessCluster(indices, clusterId, ctx);
        }

        return clusterId;
    }

    private static int ProcessCluster(
        List<int> indices, int clusterId,
        DuplicationContext ctx)
    {
        foreach (var idx in indices)
            ctx.ClusteredIndices.Add(idx);

        var clusterMethods = indices.ConvertAll(i => ctx.FileMethods[i]);
        var sharedForms = ComputeSharedForms(indices, ctx.Normalized);
        var variablePoints = ComputeVariablePoints(indices, ctx.Normalized);
        var methodMetrics = clusterMethods.ConvertAll(ComputeSimpleMetrics);

        var channel = ClassifyChannel(clusterMethods, sharedForms, variablePoints, methodMetrics);
        clusterId++;
        var dupChannel = new DuplicationChannel(
            ClusterId: clusterId, Methods: clusterMethods,
            SharedForms: sharedForms, VariablePoints: variablePoints,
            InstanceCount: clusterMethods.Count, ChannelType: channel);

        RouteToChannel(channel, dupChannel, ctx.AllHarmful, ctx.AllCaseMatrix, ctx.AllSubject);
        return clusterId;
    }

    public static void RouteToChannel(
        ChannelType channel, DuplicationChannel dupChannel,
        ICollection<DuplicationChannel> allHarmful,
        ICollection<DuplicationChannel> allCaseMatrix,
        ICollection<DuplicationChannel> allSubject)
    {
        switch (channel)
        {
            case ChannelType.Harmful: allHarmful.Add(dupChannel); break;
            case ChannelType.CaseMatrix: allCaseMatrix.Add(dupChannel); break;
            case ChannelType.Subject: allSubject.Add(dupChannel); break;
        }
    }

    private static void CollectSubjectRepetition(
        IReadOnlyList<TestMethod> fileMethods,
        IReadOnlyList<IReadOnlyList<string>> normalized,
        HashSet<int> clusteredIndices,
        ICollection<DuplicationChannel> allSubject,
        ref int clusterId)
    {
        var nonClustered = fileMethods
            .Select((m, idx) => (Method: m, Index: idx))
            .Where(x => !clusteredIndices.Contains(x.Index))
            .ToList();

        var subjectGroups = nonClustered
            .GroupBy(x => x.Method.ContainerClassName, StringComparer.Ordinal)
            .Where(g => g.Skip(2).Any());

        foreach (var subjectGroup in subjectGroups)
        {
            var subjectMethods = subjectGroup.Select(x => x.Method).ToList();
            clusterId++;
            allSubject.Add(new DuplicationChannel(
                ClusterId: clusterId,
                Methods: subjectMethods,
                SharedForms: 0,
                VariablePoints: 0, // Not meaningful for Subject channels; set to zero
                InstanceCount: subjectMethods.Count,
                ChannelType: ChannelType.Subject));
        }
    }

    /// <summary>Union-find find with path compression.</summary>
    private static int Find(int[] parent, int x)
    {
        while (parent[x] != x)
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }

        return x;
    }

    /// <summary>Union-find union.</summary>
    private static void Union(int[] parent, int a, int b)
    {
        var rootA = Find(parent, a);
        var rootB = Find(parent, b);
        if (rootA != rootB)
        {
            parent[rootB] = rootA;
        }
    }

    private static int ComputeSharedForms(
        List<int> indices,
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

    private static int ComputeVariablePoints(
        List<int> indices,
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

    public sealed record SimpleMethodMetrics(
        int LineCount,
        int AssertionCount,
        int BranchCount,
        int SetupDepth);

    private sealed record DuplicationContext(
        List<TestMethod> FileMethods,
        IReadOnlyList<IReadOnlyList<string>> Normalized,
        HashSet<int> ClusteredIndices,
        ICollection<DuplicationChannel> AllHarmful,
        ICollection<DuplicationChannel> AllCaseMatrix,
        ICollection<DuplicationChannel> AllSubject);
}
