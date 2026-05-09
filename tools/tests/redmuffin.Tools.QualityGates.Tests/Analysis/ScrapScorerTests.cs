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
