namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
/// Pure Jaccard similarity and union-find clustering over feature sets.
/// No SCRAP channel policy — only which indices belong together.
/// </summary>
public static class JaccardClustering
{
    public const double DefaultThreshold = 0.5;

    public static double Similarity(ISet<string> setA, ISet<string> setB)
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
    /// Clusters indices whose pairwise Jaccard similarity is at least
    /// <paramref name="threshold"/>. Returns only clusters with 2+ members.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<int>> FindClusters(
        IReadOnlyList<ISet<string>> featureSets,
        double threshold = DefaultThreshold)
    {
        var n = featureSets.Count;
        if (n < 2)
        {
            return [];
        }

        var parent = InitUnionFind(n);
        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                if (Similarity(featureSets[i], featureSets[j]) >= threshold)
                {
                    Union(parent, i, j);
                }
            }
        }

        var byRoot = new Dictionary<int, List<int>>();
        for (var i = 0; i < n; i++)
        {
            var root = Find(parent, i);
            if (!byRoot.TryGetValue(root, out var members))
            {
                members = [];
                byRoot[root] = members;
            }

            members.Add(i);
        }

        return byRoot.Values
            .Where(m => m.Count >= 2)
            .Select(IReadOnlyList<int> (m) => m)
            .ToList();
    }

    private static int[] InitUnionFind(int n)
    {
        var parent = new int[n];
        for (var i = 0; i < n; i++)
        {
            parent[i] = i;
        }

        return parent;
    }

    private static int Find(int[] parent, int x)
    {
        while (parent[x] != x)
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }

        return x;
    }

    private static void Union(int[] parent, int a, int b)
    {
        var rootA = Find(parent, a);
        var rootB = Find(parent, b);
        if (rootA != rootB)
        {
            parent[rootB] = rootA;
        }
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T14:10:19.2954986Z","moduleHash":"8730a9259cb17c3d54ea470e68d710e91eb8a489917bd88baa534499a382c925","forms":[{"id":"Similarity","line":10,"endLine":33,"hash":"4e68d6079200c99b5b58e73c3ddef2fcfb3dc4d80be7b0ddb8fa5f3eea5e584c"},{"id":"FindClusters","line":39,"endLine":78,"hash":"9637860db7910713fa23a3776128fb2c82ac13f0d6171659dd47a57a0c734d98"},{"id":"InitUnionFind","line":80,"endLine":89,"hash":"f6a11a571ef1990eefacb0d9c9f7c52a5197b0737d629601fe09a521fc2c17e6"},{"id":"Find","line":91,"endLine":100,"hash":"324f989491483a2158701995a0a9a7af445e59f2a0db2a9f8855727b7374fe71"},{"id":"Union","line":102,"endLine":110,"hash":"38f832404875cdf50767a3871f6cfd3d44fea592595a12155fd8f86eeae4888b"}]}
// clj-mutate-manifest-end
