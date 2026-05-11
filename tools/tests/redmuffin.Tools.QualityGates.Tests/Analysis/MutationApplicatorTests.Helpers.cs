namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public partial class MutationApplicatorTests
{
    private static string ApplyFirstMutation(string source) =>
        MutationApplicator.Apply(source, 0, MutationDiscoverer.FindSites(source)[0]);
}
