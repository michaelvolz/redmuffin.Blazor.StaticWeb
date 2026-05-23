using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.Xml.Linq;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class CoverageReaderTests
{
    [Test]
    public async Task Should_return_covered_lines_from_cobertura_xml()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "coverage-basic.xml");

        var coveredLines = CoverageReader.LoadCoverage(fixturePath);

        await Assert.That(coveredLines).IsNotNull();
        await Assert.That(coveredLines.Count).IsEqualTo(4);
        // Key format: "filename:lineNumber"
        await Assert.That(coveredLines.Contains("Math.cs:10")).IsTrue();
        await Assert.That(coveredLines.Contains("Math.cs:12")).IsTrue();
        await Assert.That(coveredLines.Contains("Math.cs:20")).IsTrue();
        await Assert.That(coveredLines.Contains("Math.cs:25")).IsTrue();
    }

    [Test]
    public async Task Should_partition_sites_by_coverage()
    {
        const string sourcePath = "SampleClass.cs";
        var coveredLines = new HashSet<string>(StringComparer.Ordinal)
        {
            "SampleClass.cs:10", "SampleClass.cs:20",
        };
        var dummyNode = await CSharpSyntaxTree.ParseText("class C {}").GetRootAsync().ConfigureAwait(false);
        var sites = new List<MutationSite>
        {
            new(0, MutationCategory.Arithmetic, 9, 0, "+ -> -", SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, dummyNode),
            new(1, MutationCategory.Comparison, 14, 0, "> -> >=", SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression, dummyNode),
            new(2, MutationCategory.Equality, 19, 0, "== -> !=", SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, dummyNode),
        };

        var (covered, uncovered) = CoverageReader.PartitionByCoverage(sites, sourcePath, coveredLines);

        await Assert.That(covered.Count).IsEqualTo(2);
        await Assert.That(uncovered.Count).IsEqualTo(1);
        await Assert.That(covered[0].Index).IsEqualTo(0);
        await Assert.That(covered[1].Index).IsEqualTo(2);
        await Assert.That(uncovered[0].Index).IsEqualTo(1);
    }

    [Test]
    public async Task Should_return_empty_set_for_empty_cobertura_xml()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "coverage-basic.xml");

        var coveredLines = CoverageReader.LoadCoverage(fixturePath);

        await Assert.That(coveredLines.Contains("Math.cs:15")).IsFalse();
        await Assert.That(coveredLines.Contains("Math.cs:22")).IsFalse();
    }

    [Test]
    public async Task Should_return_empty_partitions_for_empty_site_list()
    {
        const string sourcePath = "SampleClass.cs";
        var coveredLines = new HashSet<string>(StringComparer.Ordinal) { "SampleClass.cs:1", "SampleClass.cs:2", "SampleClass.cs:3" };
        var emptySites = Array.Empty<MutationSite>();

        var (covered, uncovered) = CoverageReader.PartitionByCoverage(emptySites, sourcePath, coveredLines);

        await Assert.That(covered.Count).IsEqualTo(0);
        await Assert.That(uncovered.Count).IsEqualTo(0);
    }
}
