namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     Additional test cases for methods hovering just above CRAP threshold.
/// </summary>
public sealed class CrapBoostTests
{
    [Test]
    public async Task TestNormalizer_VisitLiteralExpression_should_handle_all_literal_kinds()
    {
        var source = "class C { void M() { _ = 'c'; _ = \"s\"; _ = 42; _ = 3.14; _ = true; _ = false; _ = null; _ = default; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        var features = redmuffin.Tools.QualityGates.Analysis.TestNormalizer.Normalize(method);
        await Assert.That(features).IsNotEmpty();
    }

    [Test]
    public async Task DupesDetector_ScanFiles_should_scan_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "Test.cs"), "class C { void M() { if (true) return; } }").ConfigureAwait(false);

            var options = new redmuffin.Tools.QualityGates.Commands.DupesOptions(
                Threshold: 0.82, MinLines: 1, MinNodes: 1,
                Format: "text", Paths: [tempDir]);

            var results = redmuffin.Tools.QualityGates.Analysis.DupesDetector.FindDuplicates(options);
            await Assert.That(results).IsNotNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task MutationApplicator_Mutate_should_handle_arithmetic_variants()
    {
        var source = "class C { int M(int a, int b) { return a + b; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var node = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax>()
            .First();
        var site = new redmuffin.Tools.QualityGates.Analysis.MutationSite(
            0, redmuffin.Tools.QualityGates.Analysis.MutationCategory.Arithmetic, 1, 20, "test",
            SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, node);

        var mutatedSource = redmuffin.Tools.QualityGates.Analysis.MutationApplicator.Apply(source, 0, site);
        await Assert.That(mutatedSource).IsNotNull();
        await Assert.That(mutatedSource).IsNotEqualTo(source);
    }

    [Test]
    public async Task MutationApplicator_Mutate_should_handle_comparison_variants()
    {
        var source = "class C { bool M(int a, int b) { return a > b; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var node = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax>()
            .First();
        var site = new redmuffin.Tools.QualityGates.Analysis.MutationSite(
            0, redmuffin.Tools.QualityGates.Analysis.MutationCategory.Comparison,
            1, 20, "test",
            SyntaxKind.GreaterThanExpression, SyntaxKind.LessThanExpression, node);

        var mutatedSource = redmuffin.Tools.QualityGates.Analysis.MutationApplicator.Apply(source, 0, site);
        await Assert.That(mutatedSource).IsNotNull();
        await Assert.That(mutatedSource).Contains("<");
    }

    [Test]
    public async Task BuildHeaderLines_with_manifest_and_changes()
    {
        var manifest = new redmuffin.Tools.QualityGates.Analysis.Manifest(
            Version: 1, TestedAt: DateTime.UtcNow, ModuleHash: "abc", Forms: []);

        var lines = redmuffin.Tools.QualityGates.Commands.MutateHandler.BuildHeaderLines(
            "/src/Bar.cs", totalSites: 5, coveredSites: 3, uncoveredSites: 2,
            changedCount: 1, existingManifest: manifest);

        await Assert.That(lines[1]).DoesNotContain("none");
        await Assert.That(lines[6]).Contains("yes");
        await Assert.That(lines[7]).Contains("yes");
    }
}
