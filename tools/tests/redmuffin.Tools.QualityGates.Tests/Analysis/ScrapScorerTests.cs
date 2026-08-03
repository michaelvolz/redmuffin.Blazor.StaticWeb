namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class ScrapScorerTests
{
    [Test]
    public async Task should_score_simple_test_with_single_assertion()
    {
        var method = ParseMethod("""
            public void test_a()
            {
                var x = 1;
                Assert.That(x).IsNotNull();
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.AssertionCount).IsEqualTo(1);
        await Assert.That(metrics.BranchCount).IsEqualTo(0);
        await Assert.That(metrics.SmellLabels).Contains(SmellLabel.LowAssertion);
    }

    [Test]
    public async Task should_add_zero_assertion_penalty()
    {
        var method = ParseMethod("""
            public void test_empty()
            {
                var x = 1;
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.AssertionCount).IsEqualTo(0);
        await Assert.That(metrics.SmellLabels).Contains(SmellLabel.ZeroAssertion);
    }

    [Test]
    public async Task should_add_low_assertion_smell_for_one_assertion()
    {
        var method = ParseMethod("""
            public void test_one()
            {
                var x = 1;
                Assert.That(x).IsNotNull();
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.AssertionCount).IsEqualTo(1);
        await Assert.That(metrics.SmellLabels).Contains(SmellLabel.LowAssertion);
        await Assert.That(metrics.SmellLabels.Contains(SmellLabel.ZeroAssertion)).IsFalse();
    }

    [Test]
    public async Task should_not_have_assertion_smells_for_multiple_assertions()
    {
        var method = ParseMethod("""
            public void test_two()
            {
                var x = 1;
                Assert.That(x).IsNotNull();
                Assert.That(x).IsPositive();
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.AssertionCount).IsEqualTo(2);
        await Assert.That(metrics.SmellLabels.Contains(SmellLabel.ZeroAssertion)).IsFalse();
    }

    [Test]
    public async Task should_detect_branching_smell()
    {
        var method = ParseMethod("""
            public void test_branch()
            {
                var x = 1;
                if (x > 0)
                {
                    Assert.That(x).IsPositive();
                }

                Assert.That(x).IsNotNull();
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.BranchCount).IsGreaterThan(0);
        await Assert.That(metrics.SmellLabels).Contains(SmellLabel.Branching);
    }

    [Test]
    public async Task should_cap_complexity_at_25()
    {
        var statements = string.Join("\n", Enumerable.Range(0, 200).Select(i => $"        if (x == {i}) {{ Assert.That(x).IsNotNull(); }}"));
        var source = $"public void test_huge() {{\n{statements}\n    }}";
        var method = ParseMethod(source);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.ComplexityScore).IsLessThanOrEqualTo(25.0);
    }

    [Test]
    public async Task should_have_floor_complexity_of_one()
    {
        var method = ParseMethod("""
            public void test_floor()
            {
            }
            """);

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.ComplexityScore).IsGreaterThanOrEqualTo(1.0);
    }

    [Test]
    public async Task should_aggregate_file_scrap_report()
    {
        var source = """
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_a() { var x = 1; Assert.That(x).IsNotNull(); }
                [Test]
                public void test_b() { var x = 1; Assert.That(x).IsNotNull(); Assert.That(x).IsPositive(); }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "Test"))
            .Select(m => new TestMethod(m.Identifier.Text, "Test.cs", 1, 1, m.Body ?? (Microsoft.CodeAnalysis.SyntaxNode)m, "MyTests"))
            .ToList();

        var dupeResults = new DuplicationResults(
            Array.Empty<DuplicationChannel>(),
            Array.Empty<DuplicationChannel>(),
            Array.Empty<DuplicationChannel>(),
            0.0);
        var pressure = new FilePressure(0, Array.Empty<double>(), 0, 0);

        var report = ScrapScorer.ScoreFile(methods, dupeResults, pressure);

        await Assert.That(report.ExampleCount).IsEqualTo(2);
        await Assert.That(report.FilePath).IsEqualTo("Test.cs");
        await Assert.That(report.Metrics.Count).IsEqualTo(2);
    }

    [Test]
    public async Task should_compute_line_count_from_start_and_end_inclusive()
    {
        var method = new TestMethod(
            "test_span", "Test.cs", StartLine: 10, EndLine: 14,
            BodySyntax: ParseMethod("""
                public void test_span()
                {
                    Assert.That(1).IsEqualTo(1);
                    Assert.That(2).IsEqualTo(2);
                }
                """).BodySyntax,
            ContainerClassName: "TestClass");

        var metrics = ScrapScorer.ScoreMethod(method);

        await Assert.That(metrics.LineCount).IsEqualTo(5);
    }

    [Test]
    public async Task should_score_empty_file_as_zeros()
    {
        var dupeResults = new DuplicationResults([], [], [], 0.0);
        var pressure = new FilePressure(0, Array.Empty<double>(), 0, 0);

        var report = ScrapScorer.ScoreFile([], dupeResults, pressure);

        await Assert.That(report.ExampleCount).IsEqualTo(0);
        await Assert.That(report.AvgScrap).IsEqualTo(0.0);
        await Assert.That(report.MaxScrap).IsEqualTo(0.0);
        await Assert.That(report.FilePath).IsEqualTo(string.Empty);
        await Assert.That(report.SmellCounts.ZeroAssertionRatio).IsEqualTo(0.0);
        await Assert.That(report.SmellCounts.LowAssertionRatio).IsEqualTo(0.0);
        await Assert.That(report.WorstExamples).IsEmpty();
    }

    [Test]
    public async Task should_compute_smell_ratios_for_single_method_file()
    {
        // Kills exampleCount > 0 → > 1 (ratio arm never taken for one method).
        var zeroAssert = ParseMethod("""
            public void empty_assert() { var x = 1; }
            """) with { FilePath = "One.cs" };
        var lowAssert = ParseMethod("""
            public void one_assert() { Assert.That(1).IsEqualTo(1); }
            """) with { FilePath = "One.cs" };

        var zeroReport = ScrapScorer.ScoreFile(
            [zeroAssert],
            new DuplicationResults([], [], [], 0.0),
            new FilePressure(0, Array.Empty<double>(), 0, 0));
        var lowReport = ScrapScorer.ScoreFile(
            [lowAssert],
            new DuplicationResults([], [], [], 0.0),
            new FilePressure(0, Array.Empty<double>(), 0, 0));

        await Assert.That(zeroReport.ExampleCount).IsEqualTo(1);
        await Assert.That(zeroReport.SmellCounts.ZeroAssertionCount).IsEqualTo(1);
        await Assert.That(zeroReport.SmellCounts.ZeroAssertionRatio).IsEqualTo(1.0);
        await Assert.That(lowReport.SmellCounts.LowAssertionCount).IsEqualTo(1);
        await Assert.That(lowReport.SmellCounts.LowAssertionRatio).IsEqualTo(1.0);
        await Assert.That(lowReport.SmellCounts.ZeroAssertionRatio).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_apply_exact_assertion_penalties()
    {
        var zero = ScrapScorer.ScoreMethod(ParseMethod("""
            public void zero() { var x = 1; }
            """));
        var one = ScrapScorer.ScoreMethod(ParseMethod("""
            public void one() { Assert.That(1).IsEqualTo(1); }
            """));
        var two = ScrapScorer.ScoreMethod(ParseMethod("""
            public void two() {
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """));

        // Zero: +5; one: +2; two+: no assertion penalty. Base complexity is positive.
        await Assert.That(zero.ScrapScore - zero.ComplexityScore).IsEqualTo(5.0).Within(0.001);
        await Assert.That(one.ScrapScore - one.ComplexityScore).IsEqualTo(2.0).Within(0.001);
        await Assert.That(two.ScrapScore - two.ComplexityScore).IsEqualTo(0.0).Within(0.001);
        await Assert.That(zero.SmellLabels).Contains(SmellLabel.ZeroAssertion);
        await Assert.That(one.SmellLabels).Contains(SmellLabel.LowAssertion);
        await Assert.That(two.SmellLabels.Contains(SmellLabel.ZeroAssertion)).IsFalse();
        await Assert.That(two.SmellLabels.Contains(SmellLabel.LowAssertion)).IsFalse();
    }

    [Test]
    public async Task should_use_branch_count_plus_one_for_complexity()
    {
        var none = ScrapScorer.ScoreMethod(ParseMethod("""
            public void none() {
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """));
        var oneBranch = ScrapScorer.ScoreMethod(ParseMethod("""
            public void one() {
                if (true) { }
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """));

        await Assert.That(none.BranchCount).IsEqualTo(0);
        await Assert.That(oneBranch.BranchCount).IsEqualTo(1);
        // structuralComplexity = branchCount + 1 (the +1 is load-bearing).
        await Assert.That(none.ComplexityScore)
            .IsEqualTo(TestMethodMetricsCalculator.ComputeComplexityScore(1)).Within(0.0001);
        await Assert.That(oneBranch.ComplexityScore)
            .IsEqualTo(TestMethodMetricsCalculator.ComputeComplexityScore(2)).Within(0.0001);
    }

    [Test]
    public async Task should_not_flag_branching_when_branch_count_is_zero()
    {
        var metrics = ScrapScorer.ScoreMethod(ParseMethod("""
            public void linear() {
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """));

        await Assert.That(metrics.BranchCount).IsEqualTo(0);
        await Assert.That(metrics.SmellLabels.Contains(SmellLabel.Branching)).IsFalse();
    }

    [Test]
    public async Task should_not_flag_high_setup_at_depth_exactly_two()
    {
        // Setup depth counts pre-assertion statements; two statements → depth 2 → no HighSetupDepth.
        var metrics = ScrapScorer.ScoreMethod(ParseMethod("""
            public void depth_two() {
                var a = 1;
                var b = 2;
                Assert.That(a).IsEqualTo(1);
                Assert.That(b).IsEqualTo(2);
            }
            """));

        await Assert.That(metrics.SetupDepth).IsEqualTo(2);
        await Assert.That(metrics.SmellLabels.Contains(SmellLabel.HighSetupDepth)).IsFalse();
    }

    [Test]
    public async Task should_flag_high_setup_and_add_penalty_above_depth_two()
    {
        var metrics = ScrapScorer.ScoreMethod(ParseMethod("""
            public void depth_three() {
                var a = 1;
                var b = 2;
                var c = 3;
                Assert.That(a).IsEqualTo(1);
                Assert.That(b).IsEqualTo(2);
            }
            """));

        await Assert.That(metrics.SetupDepth).IsEqualTo(3);
        await Assert.That(metrics.SmellLabels).Contains(SmellLabel.HighSetupDepth);
        // Penalty = (setupDepth - 2) * 1.0; no assert penalty (2 asserts); no branches.
        await Assert.That(metrics.ScrapScore)
            .IsEqualTo(metrics.ComplexityScore + 1.0).Within(0.0001);
    }

    [Test]
    public async Task should_set_file_path_from_first_method_when_exactly_one()
    {
        var method = ParseMethod("""
            public void only() {
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """);
        // ParseMethod uses "Test.cs"; override to a distinctive path.
        method = method with { FilePath = "UniquePath.cs" };

        var report = ScrapScorer.ScoreFile(
            [method],
            new DuplicationResults([], [], [], 0.0),
            new FilePressure(0, Array.Empty<double>(), 0, 0));

        await Assert.That(report.ExampleCount).IsEqualTo(1);
        await Assert.That(report.FilePath).IsEqualTo("UniquePath.cs");
        await Assert.That(report.AvgScrap).IsEqualTo(report.MaxScrap);
    }

    [Test]
    public async Task should_add_one_point_per_branch_to_scrap_score()
    {
        var method = ScrapScorer.ScoreMethod(ParseMethod("""
            public void branches() {
                if (a) { }
                if (b) { }
                Assert.That(1).IsEqualTo(1);
                Assert.That(2).IsEqualTo(2);
            }
            """));

        await Assert.That(method.BranchCount).IsEqualTo(2);
        await Assert.That(method.SmellLabels).Contains(SmellLabel.Branching);
        // No assertion penalty (2 asserts); scrap = complexity + branchCount * 1.0
        await Assert.That(method.ScrapScore)
            .IsEqualTo(method.ComplexityScore + 2.0).Within(0.0001);
    }

    /// <summary>Parses a single method from a source snippet.</summary>
    private static TestMethod ParseMethod(string source)
    {
        var fullSource = $"using TUnit.Core; class TestClass {{ {source} }}";
        var tree = CSharpSyntaxTree.ParseText(fullSource);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var lineSpan = method.GetLocation().GetLineSpan();
        return new TestMethod(
            method.Identifier.Text,
            "Test.cs",
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.EndLinePosition.Line + 1,
            method.Body ?? (Microsoft.CodeAnalysis.SyntaxNode)method,
            "TestClass");
    }
}
