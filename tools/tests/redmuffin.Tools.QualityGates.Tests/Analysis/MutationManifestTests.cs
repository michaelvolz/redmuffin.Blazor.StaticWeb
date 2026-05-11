using TUnit.Core;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class MutationManifestTests
{
    private const string SimpleSource = """
        namespace Test;

        public class Calculator
        {
            public static int Add(int a, int b) => a + b;
            public static int Multiply(int a, int b) => a * b;
        }
        """;

    [Test]
    public async Task Should_build_manifest_with_correct_form_count()
    {
        var manifest = MutationManifest.Build(SimpleSource, DateTime.UtcNow);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest.Version).IsEqualTo(1);
        await Assert.That(manifest.ModuleHash).IsNotNull();
        await Assert.That(manifest.Forms.Count).IsEqualTo(2); // Add, Multiply
        await Assert.That(manifest.Forms[0].Id).IsEqualTo("Add");
        await Assert.That(manifest.Forms[1].Id).IsEqualTo("Multiply");
    }

    [Test]
    public async Task Should_round_trip_embed_and_extract()
    {
        var manifest = MutationManifest.Build(SimpleSource, DateTime.UtcNow);
        var embedded = MutationManifest.Embed(SimpleSource, manifest);
        var extracted = MutationManifest.Extract(embedded);

        await Assert.That(extracted).IsNotNull();
        await Assert.That(extracted!.Version).IsEqualTo(manifest.Version);
        await Assert.That(extracted.ModuleHash).IsEqualTo(manifest.ModuleHash);
        await Assert.That(extracted.Forms.Count).IsEqualTo(manifest.Forms.Count);
    }

    [Test]
    public async Task Should_strip_manifest_from_source()
    {
        var manifest = MutationManifest.Build(SimpleSource, DateTime.UtcNow);
        var embedded = MutationManifest.Embed(SimpleSource, manifest);
        var stripped = MutationManifest.Strip(embedded);

        await Assert.That(stripped).IsEqualTo(SimpleSource);
    }

    [Test]
    public async Task Should_detect_changed_forms()
    {
        var changedSource = """
            namespace Test;

            public class Calculator
            {
                public static int Add(int a, int b) => a + b;
                public static int Multiply(int a, int b) => a - b;
            }
            """;

        var oldManifest = MutationManifest.Build(SimpleSource, DateTime.UtcNow);
        var newManifest = MutationManifest.Build(changedSource, DateTime.UtcNow);

        var changed = MutationManifest.ChangedFormIndices(oldManifest, newManifest);

        await Assert.That(changed.Count).IsEqualTo(1);
        await Assert.That(changed.Contains(1)).IsTrue(); // Multiply is index 1
    }

    [Test]
    public async Task Build_handles_struct_declaration()
    {
        var code = "struct Point { public int Calc() => 1; }";
        var manifest = MutationManifest.Build(code, DateTime.UtcNow);
        await Assert.That(manifest.Forms.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Build_handles_interface_declaration()
    {
        var code = "interface IService { int GetValue(); }";
        var manifest = MutationManifest.Build(code, DateTime.UtcNow);
        await Assert.That(manifest.Forms.Count).IsEqualTo(1);
        await Assert.That(manifest.Forms[0].Id).IsEqualTo("GetValue");
    }

    [Test]
    public async Task GetMemberId_constructor_uses_default_fallback()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { public C() { } }");
        var root = tree.GetCompilationUnitRoot();
        var ctor = root.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>().First();
        var id = MutationManifest.GetMemberId(ctor);
        await Assert.That(id).IsEqualTo("member");
    }
}
