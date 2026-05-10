namespace redmuffin.Tools.QualityGates.Tests.Commands;

using Commands = redmuffin.Tools.QualityGates.Commands;
using Microsoft.CodeAnalysis.CSharp;
using ProdAnalysis = redmuffin.Tools.QualityGates.Analysis;

public sealed class MutateHandlerHelperTests
{
    [Test]
    public async Task BuildHeaderLines_should_return_all_expected_lines()
    {
        var lines = Commands.MutateHandler.BuildHeaderLines(
            sourcePath: "/src/Foo.cs",
            totalSites: 10, coveredSites: 7, uncoveredSites: 3,
            changedCount: 2, existingManifest: null);

        await Assert.That(lines).IsNotEmpty();
        await Assert.That(lines[0]).Contains("Foo.cs");
        await Assert.That(lines[1]).Contains("none");
        await Assert.That(lines[2]).Contains("10");
        await Assert.That(lines[3]).Contains("7");
        await Assert.That(lines[4]).Contains("3");
        await Assert.That(lines[5]).Contains("2");
        await Assert.That(lines[6]).Contains("no");
        await Assert.That(lines[7]).Contains("n/a");
    }

    [Test]
    public async Task BuildHeaderLines_should_report_manifest_present()
    {
        var manifest = new ProdAnalysis.Manifest(
            Version: 1, TestedAt: DateTime.UtcNow.AddHours(-1),
            ModuleHash: "abc123", Forms: []);
        var lines = Commands.MutateHandler.BuildHeaderLines(
            "/src/Foo.cs", 1, 1, 0, 0, manifest);

        await Assert.That(lines[1]).DoesNotContain("none");
        await Assert.That(lines[6]).Contains("yes");
    }

    [Test]
    public async Task ApplyDifferentialFilter_should_return_full_count_when_no_manifest()
    {
        var node = SyntaxFactory.IdentifierName("x");
        var sites = new List<ProdAnalysis.MutationSite>
        {
            new(0, ProdAnalysis.MutationCategory.Arithmetic, 10, 20, "test",
                SyntaxKind.IdentifierName, SyntaxKind.IdentifierName, node),
        };

        var count = Commands.MutateHandler.ApplyDifferentialFilter(
            sites, strippedSource: "", existingManifest: null,
            options: new Commands.MutateOptions());
        await Assert.That(count).IsEqualTo(1);
    }
}
