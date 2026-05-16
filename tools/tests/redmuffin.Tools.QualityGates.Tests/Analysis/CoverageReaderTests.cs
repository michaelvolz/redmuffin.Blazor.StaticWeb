using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml.Linq;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class CoverageReaderTests
{
    [Test]
    public async Task Should_return_covered_line_numbers_from_cobertura_xml()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "coverage-basic.xml");

        var coveredLines = CoverageReader.LoadCoverage(fixturePath);

        await Assert.That(coveredLines).IsNotNull();
        await Assert.That(coveredLines.Count).IsEqualTo(4);
        await Assert.That(coveredLines.Contains(10)).IsTrue();
        await Assert.That(coveredLines.Contains(12)).IsTrue();
        await Assert.That(coveredLines.Contains(20)).IsTrue();
        await Assert.That(coveredLines.Contains(25)).IsTrue();
    }

    [Test]
    public async Task Should_partition_sites_by_coverage()
    {
        var coveredLines = new HashSet<int> { 10, 20 };
        var dummyNode = await CSharpSyntaxTree.ParseText("class C {}").GetRootAsync().ConfigureAwait(false);
        var sites = new List<MutationSite>
        {
            new(0, MutationCategory.Arithmetic, 9, 0, "+ → -", SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, dummyNode),    // Roslyn 0-based line 9 → XML line 10 (covered)
            new(1, MutationCategory.Comparison, 14, 0, "> → >=", SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression, dummyNode), // Roslyn line 14 → XML line 15 (uncovered)
            new(2, MutationCategory.Equality, 19, 0, "== → !=", SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, dummyNode),    // Roslyn line 19 → XML line 20 (covered)
        };

        var (covered, uncovered) = CoverageReader.PartitionByCoverage(sites, coveredLines);

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

        // fixture has lines 10,12,20,25 covered (hits > 0)
        // lines 15,22 are uncovered (hits=0)
        await Assert.That(coveredLines.Contains(15)).IsFalse();
        await Assert.That(coveredLines.Contains(22)).IsFalse();
    }

    [Test]
    public async Task Should_return_empty_partitions_for_empty_site_list()
    {
        var coveredLines = new HashSet<int> { 1, 2, 3 };
        var emptySites = Array.Empty<MutationSite>();

        var (covered, uncovered) = CoverageReader.PartitionByCoverage(emptySites, coveredLines);

        await Assert.That(covered.Count).IsEqualTo(0);
        await Assert.That(uncovered.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryParseLine_should_return_false_for_element_without_attributes()
    {
        var xml = XElement.Parse("<line />");
        var ok = CoverageReader.TryParseLine(xml, out var lineNumber);
        await Assert.That(ok).IsFalse();
        await Assert.That(lineNumber).IsEqualTo(0);
    }
}
