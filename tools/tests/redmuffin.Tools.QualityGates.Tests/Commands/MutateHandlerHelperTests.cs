namespace redmuffin.Tools.QualityGates.Tests.Commands;

using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

public sealed class MutateHandlerHelperTests
{
    [Test]
    public async Task BuildSummaryLines_no_results_shows_zero()
    {
        var lines = MutateHandler.BuildSummaryLines([], []);
        await Assert.That(lines.Count).IsGreaterThan(0);
        await Assert.That(lines[1]).Contains("0/0");
    }

    [Test]
    public async Task BuildSummaryLines_with_killed_mutants()
    {
        var results = new List<MutantResult>
        {
            new(0, MutationCategory.Arithmetic, 10, "add to sub", MutantResultType.Killed, 5),
            new(1, MutationCategory.Equality, 20, "== to !=", MutantResultType.Killed, 3),
        };
        var lines = MutateHandler.BuildSummaryLines(results, []);
        await Assert.That(lines[1]).Contains("2/2");
    }

    [Test]
    public async Task BuildSummaryLines_with_survivors_lists_them()
    {
        var results = new List<MutantResult>
        {
            new(0, MutationCategory.Arithmetic, 10, "add to sub", MutantResultType.Survived, 2),
        };
        var lines = MutateHandler.BuildSummaryLines(results, []);
        await Assert.That(lines).Contains("Survivors:");
    }

    [Test]
    public async Task BuildSummaryLines_with_uncovered_shows_count()
    {
        var tree = CSharpSyntaxTree.ParseText("1 + 1");
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var dummyNode = root.DescendantNodes().First();
        var sites = new List<MutationSite>
        {
            new(0, MutationCategory.Arithmetic, 1, 1, "add",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractExpression, dummyNode),
            new(1, MutationCategory.Comparison, 2, 2, "gt",
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.GreaterThanExpression,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanExpression, dummyNode),
        };
        var lines = MutateHandler.BuildSummaryLines([], sites);
        await Assert.That(lines[2]).Contains("2 uncovered");
    }
}
