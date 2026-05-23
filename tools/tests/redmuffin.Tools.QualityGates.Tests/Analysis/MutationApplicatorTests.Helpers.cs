namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public partial class MutationApplicatorTests
{
    private static string ApplyFirstMutation(string source) =>
        MutationApplicator.Apply(source, MutationDiscoverer.FindSites(source)[0]);
}
