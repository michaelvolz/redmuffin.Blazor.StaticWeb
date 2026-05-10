using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

[Category("Feature:Dupes")]
public sealed class DupesNormalizerTests
{
    [Test]
    public async Task Normalize_PreservesStructuralShape()
    {
        var code = "class C { void M() { if (true) return; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);

        var result = DupesNormalizer.Normalize(root);
        var text = DupesNormalizer.SerializeNormalized(result);

        // The if statement should be preserved in the normalized tree
        await Assert.That(text).Contains("if");
    }

    [Test]
    public async Task ComputeFingerprints_ProducesSetOfFingerprints()
    {
        var code = "class C { void M() { int x = y + z; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);

        var normalized = DupesNormalizer.Normalize(root);
        var fingerprints = DupesNormalizer.ComputeFingerprints(normalized);

        await Assert.That(fingerprints.Count).IsGreaterThan(0);
        // Every fingerprint should be a non-empty string
        foreach (var fp in fingerprints)
        {
            await Assert.That(string.IsNullOrWhiteSpace(fp)).IsFalse();
        }
    }
}
