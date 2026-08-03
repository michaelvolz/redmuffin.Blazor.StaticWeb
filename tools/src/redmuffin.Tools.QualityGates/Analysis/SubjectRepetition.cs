namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
/// Second-pass SCRAP channel: same-class tests that never joined a Jaccard
/// cluster form Subject repetition when three or more remain.
/// </summary>
public static class SubjectRepetition
{
    /// <summary>
    /// Appends Subject channels for non-clustered methods that share a
    /// container class (≥3 methods). Returns the next available cluster id.
    /// </summary>
    public static int Collect(
        IReadOnlyList<TestMethod> fileMethods,
        ISet<int> clusteredIndices,
        ICollection<DuplicationChannel> allSubject,
        int clusterId)
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
                VariablePoints: 0, // Not meaningful for Subject channels
                InstanceCount: subjectMethods.Count,
                ChannelType: ChannelType.Subject));
        }

        return clusterId;
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T14:00:36.7597167Z","moduleHash":"4ea1313060158b236939f38f66da62edaccadfeb7ae829208566419daa518b61","forms":[{"id":"Collect","line":12,"endLine":41,"hash":"4ea1313060158b236939f38f66da62edaccadfeb7ae829208566419daa518b61"}]}
// clj-mutate-manifest-end
