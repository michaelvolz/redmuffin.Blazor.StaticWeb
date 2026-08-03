namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class DuplicationChannelClassifierTests
{
    // --- ComputeSharedForms ---

    [Test]
    public async Task SharedForms_empty_indices_returns_zero()
    {
        var actual = DuplicationChannelClassifier.ComputeSharedForms([], [["a"]]);
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task SharedForms_intersection_across_all_indices()
    {
        var normalized = new IReadOnlyList<string>[]
        {
            ["a", "b", "c"],
            ["a", "b", "d"],
            ["a", "b", "e"]
        };

        var actual = DuplicationChannelClassifier.ComputeSharedForms([0, 1, 2], normalized);

        await Assert.That(actual).IsEqualTo(2); // a, b
    }

    [Test]
    public async Task SharedForms_uses_first_index_not_second()
    {
        // If indices[0] is mutated to indices[1], shared forms become {x}∩{y}=∅.
        var normalized = new IReadOnlyList<string>[]
        {
            ["shared", "only0"],
            ["shared", "only1"]
        };

        var actual = DuplicationChannelClassifier.ComputeSharedForms([0, 1], normalized);

        await Assert.That(actual).IsEqualTo(1);
    }

    // --- ComputeVariablePoints ---

    [Test]
    public async Task VariablePoints_single_index_returns_zero()
    {
        var actual = DuplicationChannelClassifier.ComputeVariablePoints(
            [0], [["a", "b"]]);
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task VariablePoints_empty_indices_returns_zero()
    {
        var actual = DuplicationChannelClassifier.ComputeVariablePoints([], [["a"]]);
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task VariablePoints_is_union_minus_intersection()
    {
        var normalized = new IReadOnlyList<string>[]
        {
            ["a", "b"],
            ["a", "c"]
        };

        // union {a,b,c}=3, intersection {a}=1 → variable points 2
        var actual = DuplicationChannelClassifier.ComputeVariablePoints([0, 1], normalized);

        await Assert.That(actual).IsEqualTo(2);
    }

    [Test]
    public async Task VariablePoints_identical_sets_is_zero()
    {
        var normalized = new IReadOnlyList<string>[]
        {
            ["a", "b"],
            ["a", "b"]
        };

        var actual = DuplicationChannelClassifier.ComputeVariablePoints([0, 1], normalized);

        await Assert.That(actual).IsEqualTo(0);
    }

    // --- ComputeSimpleMetrics ---

    [Test]
    public async Task SimpleMetrics_line_count_is_inclusive_span()
    {
        var method = ParseMethod("void M() { var x = 1; }", startLine: 10, endLine: 12);

        var metrics = DuplicationChannelClassifier.ComputeSimpleMetrics(method);

        // EndLine - StartLine + 1 → 3 (not 2 if +1→+0, not 1 if − becomes +)
        await Assert.That(metrics.LineCount).IsEqualTo(3);
    }

    // --- ClassifyChannel boundaries ---

    [Test]
    public async Task Classify_harmful_requires_shared_forms_at_least_three()
    {
        // sharedForms=2 is not Harmful; empty metrics → AllLowComplexity true → CaseMatrix.
        var result = DuplicationChannelClassifier.ClassifyChannel(
            [], sharedForms: 2, variablePoints: 0, metrics: []);

        await Assert.That(result).IsEqualTo(ChannelType.CaseMatrix);
        await Assert.That(result).IsNotEqualTo(ChannelType.Harmful);
    }

    [Test]
    public async Task Classify_harmful_at_shared_three_and_variable_four()
    {
        // Boundaries: sharedForms >= 3 AND variablePoints <= 4 (inclusive).
        var result = DuplicationChannelClassifier.ClassifyChannel(
            [], sharedForms: 3, variablePoints: 4, metrics: []);

        await Assert.That(result).IsEqualTo(ChannelType.Harmful);
    }

    [Test]
    public async Task Classify_not_harmful_when_variable_points_five()
    {
        // sharedForms high but variablePoints 5 → not Harmful; empty metrics → Subject
        // (AllLowComplexity([]) is true → CaseMatrix actually!)
        var highComplexity = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 20, AssertionCount: 2, BranchCount: 1, SetupDepth: 3);

        var result = DuplicationChannelClassifier.ClassifyChannel(
            [], sharedForms: 5, variablePoints: 5, metrics: [highComplexity]);

        await Assert.That(result).IsEqualTo(ChannelType.Subject);
    }

    [Test]
    public async Task Classify_case_matrix_when_low_complexity_and_not_harmful()
    {
        var low = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 0, SetupDepth: 1);

        var result = DuplicationChannelClassifier.ClassifyChannel(
            [], sharedForms: 1, variablePoints: 10, metrics: [low]);

        await Assert.That(result).IsEqualTo(ChannelType.CaseMatrix);
    }

    // --- AllLowComplexity boundaries ---

    [Test]
    public async Task AllLowComplexity_line_count_twelve_is_low()
    {
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 12, AssertionCount: 1, BranchCount: 0, SetupDepth: 2);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsTrue();
    }

    [Test]
    public async Task AllLowComplexity_line_count_thirteen_is_high()
    {
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 13, AssertionCount: 1, BranchCount: 0, SetupDepth: 2);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsFalse();
    }

    [Test]
    public async Task AllLowComplexity_assertion_count_two_is_high()
    {
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 2, BranchCount: 0, SetupDepth: 0);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsFalse();
    }

    [Test]
    public async Task AllLowComplexity_branch_count_one_is_high()
    {
        // BranchCount <= 0 required; 1 is not low.
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 1, SetupDepth: 0);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsFalse();
    }

    [Test]
    public async Task AllLowComplexity_setup_depth_two_is_low()
    {
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 0, SetupDepth: 2);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsTrue();
    }

    [Test]
    public async Task AllLowComplexity_setup_depth_three_is_high()
    {
        var m = new DuplicationChannelClassifier.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 0, SetupDepth: 3);
        await Assert.That(DuplicationChannelClassifier.AllLowComplexity([m])).IsFalse();
    }

    private static TestMethod ParseMethod(string methodSource, int startLine, int endLine)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {methodSource} }}");
        var method = tree.GetCompilationUnitRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        return new TestMethod(
            MethodName: method.Identifier.Text,
            FilePath: "T.cs",
            StartLine: startLine,
            EndLine: endLine,
            BodySyntax: method.Body ?? (Microsoft.CodeAnalysis.SyntaxNode)method,
            ContainerClassName: "C");
    }
}
