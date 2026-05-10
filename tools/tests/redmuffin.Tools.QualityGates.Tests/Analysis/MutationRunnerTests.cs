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

    private static string SourcePath => Path.Combine(FixtureDir, "Calculator.cs");

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
        var source = await File.ReadAllTextAsync(SourcePath).ConfigureAwait(false);
        await Assert.That(source).Contains("a + b"); // file should be restored from previous test
        await Assert.That(source).Contains("a * b");
        var allSites = MutationDiscoverer.FindSites(source);

        // Multiply is not tested in CalculatorTests, so *→/ should survive
        var multiplySite = allSites.FirstOrDefault(s => s.Category == MutationCategory.Arithmetic && s.Description.Contains("multiplication"));
        if (multiplySite is null)
        {
            throw new InvalidOperationException(
                $"No multiply site found. Found {allSites.Count} sites: " +
                string.Join(", ", allSites.Where(s => s.Category == MutationCategory.Arithmetic).Select(s => s.Description)));
        }
        var sites = new List<MutationSite> { multiplySite };

        var results = await MutationRunner.RunAsync(SourcePath, sites, FixtureDir).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Result).IsEqualTo(MutantResultType.Survived);
    }
}
