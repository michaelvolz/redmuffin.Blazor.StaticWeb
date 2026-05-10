using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;
using Microsoft.CodeAnalysis.CSharp;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class MutationApplicatorTests
{
    [Test]
    public async Task Should_apply_arithmetic_mutation_plus_to_minus()
    {
        const string source = "class C { void M() { int x = a + b; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated).IsNotNull();
        await Assert.That(mutated.Contains('-')).IsTrue();
        await Assert.That(mutated.Contains('+')).IsFalse();
    }

    [Test]
    public async Task Should_apply_comparison_mutation_greater_to_greater_or_equal()
    {
        const string source = "class C { void M() { bool x = a > b; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated.Contains(">=")).IsTrue();
    }

    [Test]
    public async Task Should_apply_equality_mutation_equals_to_not_equals()
    {
        const string source = "class C { void M() { bool x = a == b; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated.Contains("!=")).IsTrue();
        await Assert.That(mutated.Contains("==")).IsFalse();
    }

    [Test]
    public async Task Should_apply_boolean_mutation_true_to_false()
    {
        const string source = "class C { void M() { bool x = true; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated.Contains("false")).IsTrue();
        await Assert.That(mutated.Contains("true")).IsFalse();
    }

    [Test]
    public async Task Should_apply_conditional_mutation_negate_if_condition()
    {
        const string source = "class C { void M() { if (x) { } else { } } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated.Contains("!(")).IsTrue();
    }

    [Test]
    public async Task Should_apply_constant_mutation_zero_to_one()
    {
        const string source = "class C { void M() { int x = 0; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var site = sites[0];

        var mutated = MutationApplicator.Apply(source, 0, site);

        await Assert.That(mutated.Contains("= 1")).IsTrue();
        await Assert.That(mutated.Contains("= 0")).IsFalse();
    }

    [Test]
    public async Task Should_only_mutate_target_site_in_multi_site_file()
    {
        const string source = "class C { void M() { int x = a + b; int y = c * d; } }";

        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Count).IsEqualTo(2);

        var mutated = MutationApplicator.Apply(source, 1, sites[1]);

        // Site 1 (multiply) should become divide, site 0 (add) should remain unchanged
        await Assert.That(mutated.Contains('*')).IsFalse();
        await Assert.That(mutated.Contains('/')).IsTrue();
        await Assert.That(mutated.Contains('+')).IsTrue();
    }

    [Test]
    public async Task Should_throw_for_out_of_range_index()
    {
        const string source = "class C { void M() { int x = a + b; } }";
        var sites = MutationDiscoverer.FindSites(source);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            MutationApplicator.Apply(source, 99, sites[0]);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Should_produce_parsable_output_after_mutation()
    {
        const string source = "class C { void M() { int x = a + b; } }";

        var sites = MutationDiscoverer.FindSites(source);
        var mutated = MutationApplicator.Apply(source, 0, sites[0]);

        _ = CSharpSyntaxTree.ParseText(mutated);
    }
}
