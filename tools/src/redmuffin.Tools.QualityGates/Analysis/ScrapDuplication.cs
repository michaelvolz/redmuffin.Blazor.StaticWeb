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

    /// <summary>
    /// Analyzes test methods for structural duplication using Jaccard
    /// similarity on normalized bodies. Clusters connected components
    /// and classifies into harmful, case-matrix, and subject channels.
    /// </summary>
    /// <returns></returns>
    public static DuplicationResults Analyze(IReadOnlyList<TestMethod> methods)
    {
        if (methods.Count == 0)
        {
            return new DuplicationResults(
                [],
                [],
                [],
                0.0);
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

            // Normalize all methods
            var normalized = fileMethods.ConvertAll(m =>
            {
                var methodDecl = m.BodySyntax as MethodDeclarationSyntax
                    ?? m.BodySyntax.AncestorsAndSelf()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault();

                return methodDecl is not null
                    ? TestNormalizer.Normalize(methodDecl)
                    : (IReadOnlyList<string>)[];
            });

            // Build feature sets
            var featureSets = normalized
                .ConvertAll(f => new HashSet<string>(f, StringComparer.Ordinal))
;

            // Compute pairwise Jaccard and build adjacency
            var n = fileMethods.Count;
            var edges = new List<(int a, int b)>();
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var sim = JaccardSimilarity(featureSets[i], featureSets[j]);
                    if (sim >= JaccardThreshold)
                    {
                        edges.Add((i, j));
                    }
                }
            }

            // Union-find clustering
            var parent = new int[n];
            for (var i = 0; i < n; i++)
            {
                parent[i] = i;
            }

            foreach (var (a, b) in edges)
            {
                Union(parent, a, b);
            }

            // Group indices by root
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

            // Track which methods are in Jaccard-based clusters
            var clusteredIndices = new HashSet<int>();

            // Classify non-singleton Jaccard clusters (harmful or case-matrix)
            foreach (var kvp in clusters)
            {
                var indices = kvp.Value;
                if (indices.Count < 2)
                {
                    continue;
                }

                foreach (var idx in indices)
                {
                    clusteredIndices.Add(idx);
                }

                var clusterMethods = indices.ConvertAll(i => fileMethods[i]);

                var sharedForms = ComputeSharedForms(indices, normalized);
                var variablePoints = ComputeVariablePoints(indices, normalized);
                var methodMetrics = clusterMethods.ConvertAll(ComputeSimpleMetrics);

                var channel = ClassifyChannel(
                    clusterMethods,
                    sharedForms,
                    variablePoints,
                    methodMetrics);

                clusterId++;
                var dupChannel = new DuplicationChannel(
                    ClusterId: clusterId,
                    Methods: clusterMethods,
                    SharedForms: sharedForms,
                    VariablePoints: variablePoints,
                    InstanceCount: clusterMethods.Count,
                    ChannelType: channel);

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

            // Subject repetition: non-clustered methods grouped by ContainerClassName
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
                    VariablePoints: normalized[0].Count,
                    InstanceCount: subjectMethods.Count,
                    ChannelType: ChannelType.Subject));
            }
        }

        return new DuplicationResults(
            HarmfulDuplication: allHarmful,
            CaseMatrixRepetition: allCaseMatrix,
            SubjectRepetition: allSubject,
            EffectiveDuplicationScore: 0.0);
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

    /// <summary>Union-find union by rank.</summary>
    private static void Union(int[] parent, int a, int b)
    {
        var rootA = Find(parent, a);
        var rootB = Find(parent, b);
        if (rootA != rootB)
        {
            parent[rootB] = rootA;
        }
    }

    /// <summary>
    /// Computes the count of feature tokens shared across all methods
    /// in the cluster.
    /// </summary>
    private static int ComputeSharedForms(
        List<int> indices,
        List<IReadOnlyList<string>> normalized)
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

    /// <summary>
    /// Counts feature tokens that differ across methods in the cluster.
    /// </summary>
    private static int ComputeVariablePoints(
        List<int> indices,
        List<IReadOnlyList<string>> normalized)
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

    /// <summary>Simple per-method metrics extractable from raw syntax.</summary>
    private static SimpleMethodMetrics ComputeSimpleMetrics(TestMethod method)
    {
        var body = method.BodySyntax;

        var lineCount = method.EndLine - method.StartLine + 1;

        // Count assertions: Assert.That(...) invocations
        var assertionCount = body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(i =>
            {
                if (i.Expression is MemberAccessExpressionSyntax ma)
                {
                    var exprStr = ma.Expression.ToString();
                    return exprStr.StartsWith("Assert", StringComparison.Ordinal)
                        && string.Equals(ma.Name.Identifier.Text, "That", StringComparison.Ordinal);
                }

                return false;
            });

        // Count branches: if/else/switch/while/for/foreach/conditional
        var branchCount = body.DescendantNodes().Count(n =>
            n is IfStatementSyntax
            or SwitchStatementSyntax
            or WhileStatementSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax);

        // Setup depth: statements before first assertion
        var setupDepth = 0;
        if (body is BlockSyntax block)
        {
            foreach (var stmt in block.Statements)
            {
                var hasAssert = stmt.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(i =>
                    {
                        if (i.Expression is MemberAccessExpressionSyntax ma)
                        {
                            return ma.Expression.ToString().StartsWith("Assert", StringComparison.Ordinal);
                        }

                        return false;
                    });

                if (hasAssert)
                {
                    break;
                }

                setupDepth++;
            }
        }

        return new SimpleMethodMetrics(lineCount, assertionCount, branchCount, setupDepth);
    }

    /// <summary>
    /// Classifies a cluster into Harmful, CaseMatrix, or Subject channel
    /// based on shared forms, variable points, and per-method metrics.
    /// </summary>
    private static ChannelType ClassifyChannel(
        IReadOnlyList<TestMethod> methods,
        int sharedForms,
        int variablePoints,
        IReadOnlyList<SimpleMethodMetrics> metrics)
    {
        // Subject repetition: all methods share the same ContainerClassName
        // but have different structures (not enough shared forms for harmful)
        var sameClass = methods.All(m => string.Equals(m.ContainerClassName, methods[0].ContainerClassName, StringComparison.Ordinal));

        // Harmful: ≥3 shared forms AND ≤4 variable points
        if (sharedForms >= 3 && variablePoints <= 4)
        {
            return ChannelType.Harmful;
        }

        // Case-matrix: all methods are low-complexity examples
        var allLowComplexity = metrics.All(m =>
            m.LineCount <= 12
            && m.AssertionCount <= 1
            && m.BranchCount <= 0
            && m.SetupDepth <= 2
            && (1.0 + (0.18 * m.BranchCount)) <= 18); // simplified scrap ≤ 18

        if (allLowComplexity)
        {
            return ChannelType.CaseMatrix;
        }

        // Subject repetition: same class, but neither harmful nor case-matrix
        if (sameClass)
        {
            return ChannelType.Subject;
        }

        return ChannelType.Subject;
    }

    private sealed record SimpleMethodMetrics(
        int LineCount,
        int AssertionCount,
        int BranchCount,
        int SetupDepth);
}
