namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class JaccardClusteringTests
{
    [Test]
    public async Task Similarity_single_element_identical_sets_is_one()
    {
        // Kills Count == 0 → Count == 1 on the both-empty guard (would early-return 0).
        var setA = new HashSet<string> { "only" };
        var setB = new HashSet<string> { "only" };

        var similarity = JaccardClustering.Similarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(1.0);
    }

    [Test]
    public async Task Similarity_both_empty_is_exactly_zero()
    {
        var similarity = JaccardClustering.Similarity(
            new HashSet<string>(), new HashSet<string>());

        await Assert.That(similarity).IsEqualTo(0.0);
    }

    [Test]
    public async Task FindClusters_empty_or_single_returns_empty()
    {
        await Assert.That(JaccardClustering.FindClusters([])).IsEmpty();

        var single = JaccardClustering.FindClusters(
            [new HashSet<string> { "a" }]);
        await Assert.That(single).IsEmpty();
    }

    [Test]
    public async Task FindClusters_two_identical_sets_form_one_cluster()
    {
        // Kills n < 2 → n <= 2 (would skip size-2 inputs).
        var clusters = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "a", "b", "c" },
            new HashSet<string> { "a", "b", "c" }
        ]);

        await Assert.That(clusters.Count).IsEqualTo(1);
        var members = clusters[0].OrderBy(i => i).ToList();
        await Assert.That(members.Count).IsEqualTo(2);
        await Assert.That(members[0]).IsEqualTo(0);
        await Assert.That(members[1]).IsEqualTo(1);
    }

    [Test]
    public async Task FindClusters_includes_index_zero_in_chain()
    {
        // Three identical sets: if loop starts at i=1, index 0 stays isolated.
        var clusters = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "x" },
            new HashSet<string> { "x" },
            new HashSet<string> { "x" }
        ]);

        await Assert.That(clusters.Count).IsEqualTo(1);
        var members = clusters[0].OrderBy(i => i).ToList();
        await Assert.That(members.Count).IsEqualTo(3);
        await Assert.That(members[0]).IsEqualTo(0);
        await Assert.That(members[1]).IsEqualTo(1);
        await Assert.That(members[2]).IsEqualTo(2);
    }

    [Test]
    public async Task FindClusters_clusters_at_exact_default_threshold()
    {
        // Jaccard = 0.5 for {a,b} vs {b}; default threshold is 0.5 (>= not >).
        var clusters = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "a", "b" },
            new HashSet<string> { "b" }
        ]);

        await Assert.That(clusters.Count).IsEqualTo(1);
        await Assert.That(clusters[0].Count).IsEqualTo(2);
    }

    [Test]
    public async Task FindClusters_below_threshold_stays_unclustered()
    {
        // Jaccard = 1/3 for {a,b} vs {b,c} → no cluster at 0.5.
        var clusters = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "a", "b" },
            new HashSet<string> { "b", "c" }
        ]);

        await Assert.That(clusters).IsEmpty();
    }

    [Test]
    public async Task FindClusters_custom_threshold_respected()
    {
        var atHigh = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "a", "b" },
            new HashSet<string> { "b" }
        ], threshold: 0.6);

        await Assert.That(atHigh).IsEmpty();

        var atLow = JaccardClustering.FindClusters(
        [
            new HashSet<string> { "a", "b" },
            new HashSet<string> { "b" }
        ], threshold: 0.4);

        await Assert.That(atLow.Count).IsEqualTo(1);
    }
}
