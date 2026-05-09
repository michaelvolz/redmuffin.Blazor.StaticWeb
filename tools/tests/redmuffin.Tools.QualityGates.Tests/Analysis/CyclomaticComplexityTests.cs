namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class CyclomaticComplexityTests
{
    [Test]
    public async Task should_return_cc1_for_method_with_no_branches()
    {
        var result = Analyze("""
            class Foo { void Bar() { } }
            """);

        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].Complexity).IsEqualTo(1);
    }

    [Test]
    public async Task should_count_if_statement_as_one_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { if (true) { } } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_while_loop_as_one_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { while (true) { } } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_for_loop_as_one_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { for (int i = 0; i < 10; i++) { } } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_foreach_loop_as_one_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { foreach (var x in new int[0]) { } } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_each_case_label_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { int x = 0; switch (x) { case 0: break; case 1: break; } } }
            """);

        // 1 baseline + 2 case labels = 3
        await Assert.That(result[0].Complexity).IsEqualTo(3);
    }

    [Test]
    public async Task should_count_catch_clause_as_decision_point()
    {
        var result = Analyze("""
            using System; class Foo { void Bar() { try { } catch (InvalidOperationException) { } } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_logical_and_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { bool x = true && false; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_logical_or_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { bool x = true || false; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_null_coalescing_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { string x = null ?? "default"; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_ternary_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { int x = true ? 1 : 2; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_null_conditional_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar(string s) { int? x = s?.Length; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_null_coalescing_assignment_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { string s = null; s ??= "default"; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_switch_expression_arms_as_decision_points()
    {
        var result = Analyze("""
            class Foo { void Bar() { int x = 1; string s = x switch { 1 => "one", 2 => "two" }; } }
            """);

        // 1 baseline + 2 switch arms = 3
        await Assert.That(result[0].Complexity).IsEqualTo(3);
    }

    [Test]
    public async Task should_count_pattern_and_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { bool x = 1 is > 0 and < 10; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_pattern_or_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { bool x = 1 is 0 or 1; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_count_pattern_not_as_decision_point()
    {
        var result = Analyze("""
            class Foo { void Bar() { bool x = 1 is not 0; } }
            """);

        await Assert.That(result[0].Complexity).IsEqualTo(2);
    }

    [Test]
    public async Task should_combine_multiple_branching_constructs()
    {
        var result = Analyze("""
            class Foo { void Bar() { if (true) { while (false) { for (;;) { break; } } } } }
            """);

        // 1 baseline + 1 if + 1 while + 1 for = 4
        await Assert.That(result[0].Complexity).IsEqualTo(4);
    }

    [Test]
    public async Task should_extract_method_name_correctly()
    {
        var result = Analyze("""
            class Foo { void HelloWorld() { } }
            """);

        await Assert.That(result[0].MethodName).IsEqualTo("HelloWorld");
    }

    [Test]
    public async Task should_extract_line_numbers_correctly()
    {
        var source = """
            class Foo
            {
                void Bar()
                {
                }
            }
            """;

        var result = Analyze(source);

        await Assert.That(result[0].StartLine).IsEqualTo(3);
        await Assert.That(result[0].EndLine).IsEqualTo(5);
    }

    [Test]
    public async Task should_find_multiple_methods_in_one_file()
    {
        var result = Analyze("""
            class Foo
            {
                void Bar() { }
                void Baz() { if (true) { } }
            }
            """);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Complexity).IsEqualTo(1);
        await Assert.That(result[1].Complexity).IsEqualTo(2);
    }

    /// <summary>Writes source to a temp .cs file and returns the analysis results.</summary>
    private static IReadOnlyList<MethodComplexity> Analyze(string source)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"cc_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Test.cs"), source);
        return CyclomaticComplexity.Analyze(dir);
    }
}
