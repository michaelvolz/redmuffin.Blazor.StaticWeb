namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// SCRAP duplication pipeline entry: normalize → Jaccard cluster → channel
/// classify → subject-repetition pass. Algorithm pieces live in
/// <see cref="JaccardClustering"/>, <see cref="DuplicationChannelClassifier"/>,
/// and <see cref="SubjectRepetition"/>.
/// </summary>
public static class ScrapDuplication
{
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
            var featureSets = normalized
                .Select(ISet<string> (f) => new HashSet<string>(f, StringComparer.Ordinal))
                .ToList();
            var clusters = JaccardClustering.FindClusters(featureSets);
            var ctx = new FileDuplicationContext(
                FileMethods: fileMethods,
                Normalized: normalized,
                ClusteredIndices: new HashSet<int>(),
                AllHarmful: allHarmful,
                AllCaseMatrix: allCaseMatrix,
                AllSubject: allSubject);

            clusterId = CollectJaccardChannels(clusters, ctx, clusterId);
            clusterId = SubjectRepetition.Collect(
                fileMethods, ctx.ClusteredIndices, allSubject, clusterId);
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

    private static int CollectJaccardChannels(
        IReadOnlyList<IReadOnlyList<int>> clusters,
        FileDuplicationContext ctx,
        int clusterId)
    {
        foreach (var indices in clusters)
        {
            foreach (var idx in indices)
            {
                ctx.ClusteredIndices.Add(idx);
            }

            var indexList = indices as List<int> ?? indices.ToList();
            var clusterMethods = indexList.ConvertAll(i => ctx.FileMethods[i]);
            var sharedForms = DuplicationChannelClassifier.ComputeSharedForms(indexList, ctx.Normalized);
            var variablePoints = DuplicationChannelClassifier.ComputeVariablePoints(indexList, ctx.Normalized);
            var methodMetrics = clusterMethods.ConvertAll(DuplicationChannelClassifier.ComputeSimpleMetrics);

            var channel = DuplicationChannelClassifier.ClassifyChannel(
                clusterMethods, sharedForms, variablePoints, methodMetrics);
            clusterId++;
            var dupChannel = new DuplicationChannel(
                ClusterId: clusterId,
                Methods: clusterMethods,
                SharedForms: sharedForms,
                VariablePoints: variablePoints,
                InstanceCount: clusterMethods.Count,
                ChannelType: channel);

            DuplicationChannelClassifier.RouteToChannel(
                channel, dupChannel, ctx.AllHarmful, ctx.AllCaseMatrix, ctx.AllSubject);
        }

        return clusterId;
    }

    private sealed record FileDuplicationContext(
        List<TestMethod> FileMethods,
        IReadOnlyList<IReadOnlyList<string>> Normalized,
        HashSet<int> ClusteredIndices,
        ICollection<DuplicationChannel> AllHarmful,
        ICollection<DuplicationChannel> AllCaseMatrix,
        ICollection<DuplicationChannel> AllSubject);
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T14:01:09.5998181Z","moduleHash":"230769946baf3bb9e37a96aef710a4df741c31616229c74df227a703eca8514d","forms":[{"id":"Analyze","line":12,"endLine":56,"hash":"366854c8993e59550ffd165a7d85de8d3d6b7bdec1947a1fe8d723b52010462b"},{"id":"NormalizeFileMethods","line":58,"endLine":71,"hash":"1b56e47d9fbaa46124a242d934b7ec378f6b3b13b40a4e213c526a554fd78d80"},{"id":"CollectJaccardChannels","line":73,"endLine":107,"hash":"3e12d1b62e88eaaac620ed5b44877327fc7fde8630c0463b42e77130cace71e3"}]}
// clj-mutate-manifest-end
