namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class TestNormalizerTests
{
    [Test]
    public async Task should_normalize_identical_structure_to_same_features()
    {
        var method1 = ParseMethod("public void Foo() { var x = 1; Assert.That(x).IsNotNull(); }");
        var method2 = ParseMethod("public void Bar() { var y = 2; Assert.That(y).IsNotNull(); }");

        var features1 = TestNormalizer.Normalize(method1);
        var features2 = TestNormalizer.Normalize(method2);

        await Assert.That(features1.SequenceEqual(features2)).IsTrue();
    }

    // ── LiteralFeature ──

    [Test]
    public async Task LiteralFeature_should_classify_string()
    {
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.StringLiteralExpression))
            .IsEqualTo("$str");
    }

    [Test]
    public async Task LiteralFeature_should_classify_numeric()
    {
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.NumericLiteralExpression))
            .IsEqualTo("$num");
    }

    [Test]
    public async Task LiteralFeature_should_classify_bool()
    {
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.TrueLiteralExpression))
            .IsEqualTo("$bool");
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.FalseLiteralExpression))
            .IsEqualTo("$bool");
    }

    [Test]
    public async Task LiteralFeature_should_classify_null_or_default()
    {
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.NullLiteralExpression))
            .IsEqualTo("$defnull");
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.DefaultLiteralExpression))
            .IsEqualTo("$defnull");
    }

    [Test]
    public async Task LiteralFeature_should_classify_unknown_as_lit()
    {
        await Assert.That(TestNormalizer.LiteralFeature(SyntaxKind.IdentifierToken))
            .IsEqualTo("$lit");
    }

    [Test]
    public async Task should_normalize_different_assertion_shapes_differently()
    {
        var method1 = ParseMethod("public void Foo() { Assert.That(x).IsNotNull(); }");
        var method2 = ParseMethod("public void Foo() { Assert.That(x).IsEqualTo(y); }");

        var features1 = TestNormalizer.Normalize(method1);
        var features2 = TestNormalizer.Normalize(method2);

        await Assert.That(features1.SequenceEqual(features2)).IsFalse();
    }

    [Test]
    public async Task should_return_empty_for_empty_method_body()
    {
        var method = ParseMethod("public void Foo() { }");

        var features = TestNormalizer.Normalize(method);

        await Assert.That(features).IsEmpty();
    }

    [Test]
    public async Task should_exclude_comments_from_normalized_output()
    {
        var method1 = ParseMethod("public void Foo() { /* comment */ var x = 1; Assert.That(x).IsNotNull(); }");
        var method2 = ParseMethod("public void Foo() { var x = 1; Assert.That(x).IsNotNull(); }");

        var features1 = TestNormalizer.Normalize(method1);
        var features2 = TestNormalizer.Normalize(method2);

        await Assert.That(features1.SequenceEqual(features2)).IsTrue();
    }

    [Test]
    public async Task should_return_empty_for_comment_only_body()
    {
        var method = ParseMethod("public void Foo() { /* only comment */ }");

        var features = TestNormalizer.Normalize(method);

        await Assert.That(features).IsEmpty();
    }

    [Test]
    public async Task should_contain_feature_tokens_not_empty()
    {
        var method = ParseMethod("public void Foo() { Assert.That(x).IsNotNull(); }");

        var features = TestNormalizer.Normalize(method);

        await Assert.That(features.Count).IsGreaterThan(0);
    }

    /// <summary>Parses a method declaration from a source snippet.</summary>
    private static MethodDeclarationSyntax ParseMethod(string source)
    {
        var fullSource = $"class TestClass {{ {source} }}";
        var tree = CSharpSyntaxTree.ParseText(fullSource);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();
    }

    [Test]
    public async Task should_normalize_bool_and_null_literals()
    {
        var method = ParseMethod("public void Foo() { var a = true; var b = false; var c = null; var d = default(int); }");
        var features = TestNormalizer.Normalize(method);
        await Assert.That(features).IsNotEmpty();
    }

    [Test]
    public async Task should_fall_back_to_lit_for_unknown_kinds()
    {
        var source = "class C { void M() { _ = 'c'; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>().First();
        var features = TestNormalizer.Normalize(method);
        await Assert.That(features).Contains("$lit");
    }
}
