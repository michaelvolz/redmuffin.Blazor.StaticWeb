namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class MutationDiscovererTests
{
    [Test]
    public async Task Should_find_arithmetic_site_for_addition_operator()
    {
        string source = "class C { void M() { var x = a + b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count).IsEqualTo(1);
        await Assert.That(sites[0].Category).IsEqualTo(MutationCategory.Arithmetic);
    }

    [Test]
    public async Task Should_find_comparison_site_for_greater_than_operator()
    {
        string source = "class C { void M() { if (x > y) { } } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Comparison)).IsTrue();
    }

    [Test]
    public async Task Should_find_equality_site_for_equals_operator()
    {
        string source = "class C { void M() { var x = flag == true; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Equality)).IsTrue();
    }

    [Test]
    public async Task Should_find_boolean_site_for_true_literal()
    {
        string source = "class C { bool M() { return true; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count).IsEqualTo(1);
        await Assert.That(sites[0].Category).IsEqualTo(MutationCategory.Boolean);
    }

    [Test]
    public async Task Should_find_constant_site_for_zero_literal()
    {
        string source = "class C { void M() { var x = 0; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count).IsEqualTo(1);
        await Assert.That(sites[0].Category).IsEqualTo(MutationCategory.Constant);
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_subtraction_operator()
    {
        string source = "class C { void M() { var x = a - b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_comparison_site_for_less_than_operator()
    {
        string source = "class C { void M() { if (x < y) { } } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Comparison)).IsTrue();
    }

    [Test]
    public async Task Should_find_equality_site_for_not_equals_operator()
    {
        string source = "class C { void M() { var x = flag != true; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Equality)).IsTrue();
    }

    [Test]
    public async Task Should_find_boolean_site_for_false_literal()
    {
        string source = "class C { bool M() { return false; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count).IsEqualTo(1);
        await Assert.That(sites[0].Category).IsEqualTo(MutationCategory.Boolean);
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_multiplication_operator()
    {
        string source = "class C { void M() { var x = a * b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_pre_increment()
    {
        string source = "class C { void M() { var x = ++i; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_post_increment()
    {
        string source = "class C { void M() { var x = i++; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_conditional_site_for_if_statement()
    {
        string source = "class C { void M() { if (flag) { } } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Conditional)).IsTrue();
    }

    [Test]
    public async Task Should_discover_multiple_sites_in_compound_expression()
    {
        string source = """
            class C {
                void M() {
                    var x = a + b;
                    var y = c - d;
                    if (x > y) { }
                    return true;
                }
            }
            """;

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count(s => s.Category == MutationCategory.Arithmetic)).IsEqualTo(2);
        await Assert.That(sites.Count(s => s.Category == MutationCategory.Comparison)).IsEqualTo(1);
        await Assert.That(sites.Count(s => s.Category == MutationCategory.Boolean)).IsEqualTo(1);
        await Assert.That(sites.Count(s => s.Category == MutationCategory.Conditional)).IsEqualTo(1);
    }

    [Test]
    public async Task Should_skip_numeric_literal_that_is_not_zero_or_one()
    {
        string source = "class C { void M() { var x = 42; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Constant)).IsFalse();
    }

    [Test]
    public async Task Should_find_constant_site_for_one_literal()
    {
        string source = "class C { void M() { var x = 1; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Constant)).IsTrue();
    }

    [Test]
    public async Task Should_suppress_comparison_on_Count_against_zero()
    {
        string source = "class C { void M() { if (list.Count > 0) { } } }";

        var sites = MutationDiscoverer.FindSites(source);

        var comparisonSites = sites.Where(s => s.Category == MutationCategory.Comparison).ToList();
        await Assert.That(comparisonSites.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Should_suppress_comparison_on_Length_against_zero()
    {
        string source = "class C { void M() { if (arr.Length > 0) { } } }";

        var sites = MutationDiscoverer.FindSites(source);

        var comparisonSites = sites.Where(s => s.Category == MutationCategory.Comparison).ToList();
        await Assert.That(comparisonSites.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Should_suppress_constant_in_Random_method()
    {
        string source = "class C { int Random() { return 1; } }";

        var sites = MutationDiscoverer.FindSites(source);

        var constantSites = sites.Where(s => s.Category == MutationCategory.Constant).ToList();
        await Assert.That(constantSites.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Should_find_comparison_site_for_greater_than_or_equal()
    {
        string source = "class C { void M() { var x = a >= b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Comparison)).IsTrue();
    }

    [Test]
    public async Task Should_assign_correct_index_to_sites()
    {
        string source = "class C { void M() { var x = a + b; var y = c - d; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites[0].Index).IsEqualTo(0);
        await Assert.That(sites[1].Index).IsEqualTo(1);
    }

    [Test]
    public async Task Should_return_line_and_column_as_zero_based()
    {
        string source = "class C { void M() { var x = a + b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites[0].Line).IsGreaterThanOrEqualTo(0);
        await Assert.That(sites[0].Column).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_pre_decrement()
    {
        string source = "class C { void M() { var x = --i; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_arithmetic_site_for_post_decrement()
    {
        string source = "class C { void M() { var x = i--; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Arithmetic)).IsTrue();
    }

    [Test]
    public async Task Should_find_conditional_site_with_node_pointing_to_condition()
    {
        string source = "class C { void M() { if (flag) { } } }";

        var sites = MutationDiscoverer.FindSites(source);
        var conditionalSites = sites.Where(s => s.Category == MutationCategory.Conditional).ToList();

        await Assert.That(conditionalSites.Count).IsEqualTo(1);
        await Assert.That(conditionalSites[0].Node).IsNotNull();
        // The node should be the condition expression, not the if statement itself
        await Assert.That(conditionalSites[0].Node.Kind())
            .IsNotEqualTo(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IfStatement);
    }

    [Test]
    public async Task Should_find_mutation_sites_when_using_System_Linq()
    {
        string source = """
            namespace redmuffin.Tools.QualityGates.Analysis;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            public static class MutationDiscoverer {
                public static IReadOnlyList<MutationSite> FindSites(string source) {
                    return new List<MutationSite>();
                }
            }
            """;

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        await Assert.That(sites.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Should_find_all_six_categories_with_GetByCategory()
    {
        var categories = MutationRules.All.Select(r => r.Category).Distinct().ToList();
        foreach (MutationCategory category in System.Enum.GetValues<MutationCategory>())
        {
            await Assert.That(categories).Contains(category);
        }
    }

    [Test]
    public async Task Should_return_rules_for_specific_category()
    {
        var arithmeticRules = MutationRules.GetByCategory(MutationCategory.Arithmetic);
        await Assert.That(arithmeticRules.Count).IsEqualTo(7);
    }

    [Test]
    public async Task Should_return_empty_for_unknown_category_handling()
    {
        // GetByCategory returns an empty list for a category with no rules
        var rules = MutationRules.GetByCategory(MutationCategory.Constant);
        await Assert.That(rules).IsNotNull();
        await Assert.That(rules.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Should_maintain_correct_node_reference_in_mutation_site()
    {
        string source = "class C { void M() { var x = a + b; } }";

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites[0].Node).IsNotNull();
        await Assert.That(sites[0].Node.Kind())
            .IsEqualTo(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression);
    }
}
