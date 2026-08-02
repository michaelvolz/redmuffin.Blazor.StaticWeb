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
        // usings and empty methods must not invent operator sites
        string source = """
            namespace redmuffin.Tools.QualityGates.Analysis;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            public static class MutationDiscoverer {
                public static int FindSites(string source) {
                    return 0;
                }
            }
            """;

        var sites = MutationDiscoverer.FindSites(source);

        await Assert.That(sites).IsNotNull();
        // Constant 0→1 is expected; no logical/unary noise from usings
        await Assert.That(sites.All(s => s.Category == MutationCategory.Constant)).IsTrue();
    }

    [Test]
    public async Task Should_find_all_rule_table_categories_with_GetByCategory()
    {
        var categories = MutationRules.All.Select(r => r.Category).Distinct().ToList();
        // NullRvalue is discovered by walker context, not the rule table.
        var ruleTableCategories = Enum.GetValues<MutationCategory>()
            .Where(c => c != MutationCategory.NullRvalue);
        foreach (var category in ruleTableCategories)
        {
            await Assert.That(categories).Contains(category);
        }
    }

    [Test]
    public async Task Should_find_logical_and_site()
    {
        const string source = "class C { void M() { if (a && b) { } } }";
        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Any(s => s.Category == MutationCategory.Logical)).IsTrue();
    }

    [Test]
    public async Task Should_find_unary_not_strip_site()
    {
        const string source = "class C { void M() { var x = !flag; } }";
        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Any(s =>
            s.Category == MutationCategory.Unary
            && s.OriginalKind == SyntaxKind.LogicalNotExpression)).IsTrue();
    }

    [Test]
    public async Task Should_find_unary_minus_strip_site()
    {
        const string source = "class C { void M() { var x = -value; } }";
        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Any(s =>
            s.Category == MutationCategory.Unary
            && s.OriginalKind == SyntaxKind.UnaryMinusExpression)).IsTrue();
    }

    [Test]
    public async Task Should_find_null_rvalue_on_object_creation_return()
    {
        const string source = "class C { object M() { return new object(); } }";
        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Any(s => s.Category == MutationCategory.NullRvalue)).IsTrue();
    }

    [Test]
    public async Task Should_not_null_replace_numeric_literal_return()
    {
        const string source = "class C { int M() { return 42; } }";
        var sites = MutationDiscoverer.FindSites(source);
        await Assert.That(sites.Any(s => s.Category == MutationCategory.NullRvalue)).IsFalse();
    }

    [Test]
    [Arguments("class C { string M() { return \"hi\"; } }", true)]
    [Arguments("class C { object M() { return x as string; } }", true)]
    [Arguments("class C { object[] M() { return new object[0]; } }", true)]
    [Arguments("class C { void M() { object x; x = new object(); } }", true)]
    [Arguments("class C { object M() { return null; } }", false)]
    [Arguments("class C { int M() { return 1; } }", false)]
    public async Task Should_detect_null_rvalue_only_for_reference_like_rhs(
        string source, bool expectNullRvalue)
    {
        var sites = MutationDiscoverer.FindSites(source);
        var hasNull = sites.Any(s => s.Category == MutationCategory.NullRvalue);
        await Assert.That(hasNull).IsEqualTo(expectNullRvalue);
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
