using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

[NotInParallel]
public sealed class MutationRunnerTests
{
    private static string FixtureDir => Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "MutationTarget");

    private static string SurvivorFixtureDir => Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "SurvivorTarget");

    private static string SourcePath => Path.Combine(FixtureDir, "Calculator.cs");
    private static string SurvivorSourcePath => Path.Combine(SurvivorFixtureDir, "Survivor.cs");

    [Test]
    public async Task Should_kill_arithmetic_mutation_when_test_catches_it()
    {
        var source = await File.ReadAllTextAsync(SourcePath).ConfigureAwait(false);
        var allSites = MutationDiscoverer.FindSites(source);

        // The Add(a,b) => a+b test should kill the +→- mutation
        var addSite = allSites.First(s => s.Category == MutationCategory.Arithmetic && s.Description.Contains("addition"));
        var sites = new List<MutationSite> { addSite };

        var results = await MutationRunner.RunAsync(SourcePath, sites, FixtureDir).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Result).IsEqualTo(MutantResultType.Killed);

        // Verify the source file was restored after mutation
        var restored = await File.ReadAllTextAsync(SourcePath).ConfigureAwait(false);
        await Assert.That(restored).Contains("a + b");
    }

    [Test]
    public async Task Should_report_survived_for_mutation_not_covered_by_tests()
    {
        var source = await File.ReadAllTextAsync(SurvivorSourcePath).ConfigureAwait(false);
        var allSites = MutationDiscoverer.FindSites(source);

        // Multiply is not tested in SurvivorTests, so *→/ should survive
        var multiplySite = allSites.First(s =>
            s.Category == MutationCategory.Arithmetic && s.Description.Contains("multiplication"));
        var sites = new List<MutationSite> { multiplySite };

        var results = await MutationRunner.RunAsync(SurvivorSourcePath, sites, SurvivorFixtureDir)
            .ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Result).IsEqualTo(MutantResultType.Survived);
    }
}
