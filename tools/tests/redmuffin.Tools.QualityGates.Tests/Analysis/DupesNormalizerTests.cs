using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

[Category("Feature:Dupes")]
public sealed partial class DupesNormalizerTests
{
    [Test]
    public async Task Normalize_PreservesStructuralShape()
    {
        var result = await ParseNormalized("class C { void M() { if (true) return; } }").ConfigureAwait(false);
        var text = DupesNormalizer.SerializeNormalized(result);
        await Assert.That(text).Contains("if");
    }

    [Test]
    public async Task ComputeFingerprints_ProducesSetOfFingerprints()
    {
        var fingerprints = await ParseFingerprints("class C { void M() { int x = y + z; } }").ConfigureAwait(false);
        await Assert.That(fingerprints.Count).IsGreaterThan(0);
        foreach (var fp in fingerprints)
            await Assert.That(string.IsNullOrWhiteSpace(fp)).IsFalse();
    }

    [Test]
    public async Task Normalize_handles_binary_expression()
    {
        var result = await ParseNormalized("class C { void M() { var x = a + b; } }").ConfigureAwait(false);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_literal_and_symbol()
    {
        var code = "class C { int M() { return 42; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_invocation()
    {
        var code = "class C { void M() { Console.WriteLine(\"hello\"); } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_ternary_conditional()
    {
        var code = "class C { int M(bool b) { return b ? 1 : 0; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_for_loop()
    {
        var code = "class C { void M() { for (int i = 0; i < 10; i++) {} } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_foreach_loop()
    {
        var code = "class C { void M() { foreach (var x in list) {} } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_while_switch_try_throw_and_assignment()
    {
        var code = """
            class C {
                void M(int x) {
                    while (x > 0) { x--; }
                    switch (x) {
                        case 1: break;
                        default: break;
                    }
                    try { Do(); }
                    catch { throw new System.Exception(); }
                    finally { Done(); }
                    x = 1;
                }
            }
            """;
        var result = await ParseNormalized(code).ConfigureAwait(false);
        var text = DupesNormalizer.SerializeNormalized(result);
        await Assert.That(text).Contains("while");
        await Assert.That(text).Contains("switch");
        await Assert.That(text).Contains("try");
    }

    [Test]
    public async Task Normalize_handles_file_scoped_namespace_and_prefix_unary()
    {
        var code = """
            namespace Demo;
            class C {
                int M(int x) { return -x; }
            }
            """;
        var result = await ParseNormalized(code).ConfigureAwait(false);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Normalize_handles_expression_bodied_method()
    {
        var code = "class C { int M(int x) => x + 1; }";
        var result = await ParseNormalized(code).ConfigureAwait(false);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task TryComputeMethodFingerprints_succeeds_for_abstract_method_shape()
    {
        // Bodyless methods normalize without throwing; DupesDetector skips them
        // earlier via method.Body == null before calling this API.
        var code = "abstract class C { abstract void M(); }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        var ok = DupesNormalizer.TryComputeMethodFingerprints(method, out var fingerprints);

        await Assert.That(ok).IsTrue();
        await Assert.That(fingerprints).IsNotNull();
    }

    [Test]
    public async Task Normalize_handles_switch_throw_try()
    {
        var code = @"
class C {
    void M(int x) {
        switch (x) { case 1: break; }
        try { } catch { }
    }
}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var result = DupesNormalizer.Normalize(root);
        await Assert.That(result.Children.Count).IsGreaterThan(0);
    }
}
