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
    public async Task BuildSummaryLines_excludes_no_ops_from_kill_rate()
    {
        var results = new List<MutantResult>
        {
            new(0, MutationCategory.Arithmetic, 10, "add to sub", MutantResultType.Killed, 5),
            new(1, MutationCategory.Arithmetic, 11, "i++", MutantResultType.NoOp, 0),
            new(2, MutationCategory.Constant, 12, "1->0", MutantResultType.NoOp, 0),
        };
        var lines = MutateHandler.BuildSummaryLines(results, []);
        await Assert.That(lines[1]).Contains("1/1");
        await Assert.That(lines[1]).Contains("100.0%");
        await Assert.That(lines.Any(l => l.Contains("2 apply no-ops"))).IsTrue();
        await Assert.That(lines).Contains("Apply no-ops:");
    }

    [Test]
    public async Task ParseLines_parses_comma_separated_and_skips_junk()
    {
        var set = MutateCommand.ParseLines("10, 20,x,30");
        await Assert.That(set).IsNotNull();
        await Assert.That(set!).Count().IsEqualTo(3);
        await Assert.That(set.Contains(10)).IsTrue();
        await Assert.That(set.Contains(20)).IsTrue();
        await Assert.That(set.Contains(30)).IsTrue();
    }

    [Test]
    public async Task ParseLines_returns_null_for_empty()
    {
        await Assert.That(MutateCommand.ParseLines(null)).IsNull();
        await Assert.That(MutateCommand.ParseLines("  ")).IsNull();
        await Assert.That(MutateCommand.ParseLines("a,b")).IsNull();
    }

    [Test]
    public async Task WarnIfSiteCountHigh_writes_when_count_exceeds_threshold()
    {
        using var writer = new StringWriter();
        await MutateHandler.WarnIfSiteCountHighAsync(51, 50, writer).ConfigureAwait(false);
        await Assert.That(writer.ToString()).Contains("WARNING");
        await Assert.That(writer.ToString()).Contains("51");
        await Assert.That(writer.ToString()).Contains("50");
    }

    [Test]
    public async Task WarnIfSiteCountHigh_silent_when_at_or_below_threshold()
    {
        using var writer = new StringWriter();
        await MutateHandler.WarnIfSiteCountHighAsync(50, 50, writer).ConfigureAwait(false);
        await Assert.That(writer.ToString()).IsEmpty();
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

    // ── ClassifyAndFilterSites ──

    private static MutationSite MakeSite(int index, int line)
    {
        var dummyNode = CSharpSyntaxTree.ParseText("class C{}").GetRoot();
        return new MutationSite(
            index, MutationCategory.Arithmetic, line, 1, "test",
            SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, dummyNode);
    }

    [Test]
    public async Task should_partition_sites_by_coverage()
    {
        var sites = new List<MutationSite> { MakeSite(0, 10), MakeSite(1, 20) };
        var coveredLines = new HashSet<string>(StringComparer.Ordinal) { "source:11" };

        var result = MutateHandler.ClassifyAndFilterSites(
            sites, coveredLines, "source", "source", null, new MutateOptions());

        await Assert.That(result.Sites.Count).IsEqualTo(1);
        await Assert.That(result.Covered.Count).IsEqualTo(1);
        await Assert.That(result.Uncovered.Count).IsEqualTo(1);
    }

    [Test]
    public async Task should_fallback_to_all_sites_when_none_covered()
    {
        var sites = new List<MutationSite> { MakeSite(0, 10) };
        var coveredLines = new HashSet<string>(StringComparer.Ordinal);

        var result = MutateHandler.ClassifyAndFilterSites(
            sites, coveredLines, "source", "source", null, new MutateOptions());

        await Assert.That(result.Sites.Count).IsEqualTo(1);
        await Assert.That(result.Covered.Count).IsEqualTo(1);
        await Assert.That(result.Uncovered.Count).IsEqualTo(0);
    }

    [Test]
    public async Task should_filter_by_lines_option()
    {
        var sites = new List<MutationSite> { MakeSite(0, 10), MakeSite(1, 20), MakeSite(2, 30) };
        var coveredLines = new HashSet<string>(StringComparer.Ordinal) { "source:11", "source:21", "source:31" };
        var options = new MutateOptions { Lines = new HashSet<int> { 10, 30 } };

        var result = MutateHandler.ClassifyAndFilterSites(
            sites, coveredLines, "source", "source", null, options);

        await Assert.That(result.Sites.Count).IsEqualTo(2);
        await Assert.That(result.Sites[0].Line).IsEqualTo(10);
        await Assert.That(result.Sites[1].Line).IsEqualTo(30);
    }

    // ── ResolveCoverageLines helpers ──

    [Test]
    public async Task ShouldSkipCoverageGeneration_returns_true_when_coverage_exists()
    {
        var result = MutateHandler.ShouldSkipCoverageGeneration(
            new HashSet<string>(StringComparer.Ordinal) { "Foo.cs:10" }, new MutateOptions { AutoCoverage = true });
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ShouldSkipCoverageGeneration_returns_true_when_auto_disabled()
    {
        var result = MutateHandler.ShouldSkipCoverageGeneration(
            new HashSet<string>(StringComparer.Ordinal), new MutateOptions { AutoCoverage = false });
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ShouldSkipCoverageGeneration_returns_false_when_empty_and_auto()
    {
        var result = MutateHandler.ShouldSkipCoverageGeneration(
            new HashSet<string>(StringComparer.Ordinal), new MutateOptions { AutoCoverage = true });
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task WasCoverageGenerated_returns_true_for_path()
    {
        await Assert.That(MutateHandler.WasCoverageGenerated("/tmp/cov.xml")).IsTrue();
    }

    [Test]
    public async Task WasCoverageGenerated_returns_false_for_null()
    {
        await Assert.That(MutateHandler.WasCoverageGenerated(null)).IsFalse();
    }

    [Test]
    public async Task ResolveCoverageLinesAsync_returns_early_when_coverage_exists()
    {
        using var writer = new StringWriter();
        var result = await MutateHandler.ResolveCoverageLinesAsync(
                "/fake/project", new MutateOptions(), new HashSet<string>(StringComparer.Ordinal) { "Foo.cs:10" }, writer)
            .ConfigureAwait(false);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ResolveCoverageLinesAsync_handles_null_from_generator()
    {
        using var writer = new StringWriter();
        var result = await MutateHandler.ResolveCoverageLinesAsync(
                "/fake/project",
                new MutateOptions { AutoCoverage = true },
                    new HashSet<string>(StringComparer.Ordinal),
                writer,
                generateCoverage: _ => Task.FromResult<string?>(null))
            .ConfigureAwait(false);

        await Assert.That(result.Count).IsEqualTo(0);
        await Assert.That(writer.ToString()).Contains("Warning");
    }

    [Test]
    public async Task ResolveCoverageLinesAsync_loads_coverage_on_success()
    {
        var src = Path.GetTempFileName();
        var destRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(destRoot);

        try
        {
            await File.WriteAllTextAsync(src, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage line-rate="1" branch-rate="1" version="1.9">
                  <packages><package>
                    <classes>
                      <class name="Foo" filename="Foo.cs">
                        <lines><line number="10" hits="3" branch="false"/></lines>
                      </class>
                    </classes>
                  </package>                    </packages>
                </coverage>
                """).ConfigureAwait(false);

            using var writer = new StringWriter();
            var result = await MutateHandler.ResolveCoverageLinesAsync(
                    destRoot,
                    new MutateOptions { AutoCoverage = true },
                new HashSet<string>(StringComparer.Ordinal),
                    writer,
                    generateCoverage: _ => Task.FromResult<string?>(src))
                .ConfigureAwait(false);

            await Assert.That(result.Contains("Foo.cs:10")).IsTrue();
        }
        finally
        {
            File.Delete(src);
            if (Directory.Exists(destRoot))
                Directory.Delete(destRoot, recursive: true);
        }
    }
}
