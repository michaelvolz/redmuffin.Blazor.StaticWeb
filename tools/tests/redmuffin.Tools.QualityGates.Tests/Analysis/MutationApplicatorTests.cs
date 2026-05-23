using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;
using Microsoft.CodeAnalysis.CSharp;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed partial class MutationApplicatorTests
{
    [Test]
    public async Task Should_apply_arithmetic_mutation_plus_to_minus()
    {
        var mutated = ApplyFirstMutation("class C { void M() { int x = a + b; } }");
        await Assert.That(mutated).IsNotNull();
        await Assert.That(mutated.Contains('-')).IsTrue();
        await Assert.That(mutated.Contains('+')).IsFalse();
    }

    [Test]
    public async Task Should_apply_comparison_mutation_greater_to_greater_or_equal()
    {
        var mutated = ApplyFirstMutation("class C { void M() { bool x = a > b; } }");
        await Assert.That(mutated.Contains(">=")).IsTrue();
    }

    [Test]
    public async Task Should_apply_equality_mutation_equals_to_not_equals()
    {
        var mutated = ApplyFirstMutation("class C { void M() { bool x = a == b; } }");
        await Assert.That(mutated.Contains("!=")).IsTrue();
        await Assert.That(mutated.Contains("==")).IsFalse();
    }

    [Test]
    public async Task Should_apply_boolean_mutation_true_to_false()
    {
        var mutated = ApplyFirstMutation("class C { void M() { bool x = true; } }");
        await Assert.That(mutated.Contains("false")).IsTrue();
        await Assert.That(mutated.Contains("true")).IsFalse();
    }

    [Test]
    public async Task Should_apply_conditional_mutation_negate_if_condition()
    {
        var mutated = ApplyFirstMutation("class C { void M() { if (x) { } else { } } }");
        await Assert.That(mutated.Contains("!(")).IsTrue();
    }

    [Test]
    public async Task Should_apply_constant_mutation_zero_to_one()
    {
        var mutated = ApplyFirstMutation("class C { void M() { int x = 0; } }");
        await Assert.That(mutated.Contains("= 1")).IsTrue();
        await Assert.That(mutated.Contains("= 0")).IsFalse();
    }

    [Test]
    public async Task Should_only_mutate_target_site_in_multi_site_file()
    {
        const string source = "class C { void M() { int x = a + b; int y = c * d; } }";

        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Count).IsEqualTo(2);

        var mutated = MutationApplicator.Apply(source, sites[1]);

        await Assert.That(mutated.Contains('*')).IsFalse();
        await Assert.That(mutated.Contains('/')).IsTrue();
        await Assert.That(mutated.Contains('+')).IsTrue();
    }

    [Test]
    public async Task Should_produce_parsable_output_after_mutation()
    {
        var mutated = ApplyFirstMutation("class C { void M() { int x = a + b; } }");
        _ = CSharpSyntaxTree.ParseText(mutated);
    }
}
