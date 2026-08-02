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

    [Test]
    public async Task Should_mutate_numeric_literal_inside_method_argument()
    {
        const string source = "class C { void M() { TryConsume(1); } void TryConsume(int n) { } }";
        var sites = MutationDiscoverer.FindSites(source);
        var constantSite = sites.First(s => s.Category == MutationCategory.Constant);
        var mutated = MutationApplicator.Apply(source, constantSite);

        await Assert.That(mutated).IsNotEqualTo(source);
        await Assert.That(mutated.Contains("TryConsume(0)")).IsTrue();
    }

    [Test]
    public async Task Should_mutate_binary_expression_inside_method_argument()
    {
        const string source = "class C { void M() { Skip(index + 1); } void Skip(int n) { } }";
        var sites = MutationDiscoverer.FindSites(source);
        var arithmeticSite = sites.First(s => s.Category == MutationCategory.Arithmetic);
        var mutated = MutationApplicator.Apply(source, arithmeticSite);

        await Assert.That(mutated).IsNotEqualTo(source);
        await Assert.That(mutated.Contains("index - 1") || mutated.Contains("index- 1") || mutated.Contains("index -1"))
            .IsTrue();
    }

    [Test]
    public async Task Should_mutate_string_concatenation_inside_method_argument()
    {
        const string source = """class C { void M() { flags.Add("-" + token); } }""";
        var sites = MutationDiscoverer.FindSites(source);
        var arithmeticSite = sites.First(s => s.Category == MutationCategory.Arithmetic);
        var mutated = MutationApplicator.Apply(source, arithmeticSite);

        await Assert.That(mutated).IsNotEqualTo(source);
        await Assert.That(mutated.Contains("\"-\" + token")).IsFalse();
        await Assert.That(mutated.Contains("\"-\" -")).IsTrue();
    }

    [Test]
    public async Task Should_mutate_post_increment_to_post_decrement()
    {
        const string source = "class C { void M() { int i = 0; i++; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var incSite = sites.First(s => s.OriginalKind == SyntaxKind.PostIncrementExpression);
        var mutated = MutationApplicator.Apply(source, incSite);

        await Assert.That(mutated).IsNotEqualTo(source);
        await Assert.That(mutated.Contains("i--")).IsTrue();
        await Assert.That(mutated.Contains("i++")).IsFalse();
    }

    [Test]
    public async Task Should_mutate_pre_decrement_to_pre_increment()
    {
        const string source = "class C { void M() { int i = 0; --i; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var decSite = sites.First(s => s.OriginalKind == SyntaxKind.PreDecrementExpression);
        var mutated = MutationApplicator.Apply(source, decSite);

        await Assert.That(mutated).IsNotEqualTo(source);
        await Assert.That(mutated.Contains("++i")).IsTrue();
    }

    [Test]
    public async Task Should_mutate_logical_and_to_or()
    {
        const string source = "class C { void M() { bool x = a && b; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var logical = sites.First(s => s.Category == MutationCategory.Logical);
        var mutated = MutationApplicator.Apply(source, logical);

        await Assert.That(mutated.Contains("||")).IsTrue();
        await Assert.That(mutated.Contains("&&")).IsFalse();
    }

    [Test]
    public async Task Should_strip_logical_not()
    {
        const string source = "class C { void M() { bool x = !flag; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var unary = sites.First(s => s.Category == MutationCategory.Unary);
        var mutated = MutationApplicator.Apply(source, unary);

        await Assert.That(mutated.Contains("!flag")).IsFalse();
        await Assert.That(mutated.Contains("flag")).IsTrue();
    }

    [Test]
    public async Task Should_replace_object_creation_with_null()
    {
        const string source = "class C { object M() { return new object(); } }";
        var sites = MutationDiscoverer.FindSites(source);
        var nullSite = sites.First(s => s.Category == MutationCategory.NullRvalue);
        var mutated = MutationApplicator.Apply(source, nullSite);

        await Assert.That(mutated.Contains("return null")).IsTrue();
        await Assert.That(mutated.Contains("new object()")).IsFalse();
    }

    [Test]
    public async Task Should_replace_string_literal_return_with_null()
    {
        const string source = "class C { string M() { return \"hi\"; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var nullSite = sites.First(s => s.Category == MutationCategory.NullRvalue);
        var mutated = MutationApplicator.Apply(source, nullSite);

        await Assert.That(mutated.Contains("return null")).IsTrue();
        await Assert.That(mutated.Contains("\"hi\"")).IsFalse();
    }

    [Test]
    public async Task Should_mutate_logical_or_to_and()
    {
        const string source = "class C { void M() { bool x = a || b; } }";
        var sites = MutationDiscoverer.FindSites(source);
        var logical = sites.First(s => s.Category == MutationCategory.Logical);
        var mutated = MutationApplicator.Apply(source, logical);

        await Assert.That(mutated.Contains("&&")).IsTrue();
        await Assert.That(mutated.Contains("||")).IsFalse();
    }
}
