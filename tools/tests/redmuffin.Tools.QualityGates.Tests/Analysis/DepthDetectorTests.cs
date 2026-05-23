using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class DepthDetectorTests
{
    private static string DepthFixturesDir => Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "depth-fixtures");

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_shallow_private_method_with_no_branching()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.FirstOrDefault(r => r.MethodName == "ShallowHelper");

        await Assert.That(shallow).IsNotNull();
        await Assert.That(shallow!.IsShallow).IsTrue();
        await Assert.That(shallow.CompositeScore).IsEqualTo(3);
        await Assert.That(shallow.Signals).Contains("shallow(3)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_parameter_bloat_over_four_params()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var bloat = results.FirstOrDefault(r => r.MethodName == "Configure");

        await Assert.That(bloat).IsNotNull();
        await Assert.That(bloat!.ParameterCount).IsEqualTo(5);
        await Assert.That(bloat.CompositeScore).IsEqualTo(1);
        await Assert.That(bloat.Signals).Contains("params(1)");
        await Assert.That(bloat.IsShallow).IsFalse(); // public — not shallow
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_wrong_abstraction_branching_on_parameter()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var wrong = results.FirstOrDefault(r => r.MethodName == "ApplyMode");

        await Assert.That(wrong).IsNotNull();
        await Assert.That(wrong!.IsWrongAbstraction).IsTrue();
        await Assert.That(wrong.CompositeScore).IsEqualTo(2);
        await Assert.That(wrong.Signals).Contains("wrong-abstraction(2)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_combined_shallow_and_param_bloat()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var combined = results.FirstOrDefault(r => r.MethodName == "Combine");

        await Assert.That(combined).IsNotNull();
        await Assert.That(combined!.IsShallow).IsTrue();
        await Assert.That(combined.ParameterCount).IsEqualTo(5);
        await Assert.That(combined.CompositeScore).IsEqualTo(4);
        await Assert.That(combined.Signals).Contains("shallow(3)");
        await Assert.That(combined.Signals).Contains("params(1)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_flag_deep_methods_with_branching_and_loc_over_four()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var compute = results.FirstOrDefault(r => r.MethodName == "Compute");
        var process = results.FirstOrDefault(r => r.MethodName == "Process");

        await Assert.That(compute).IsNull(); // public, LOC>4, branching → composite=0, excluded
        await Assert.That(process).IsNull(); // private but LOC>4, loops+branching → composite=0, excluded
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_flag_public_constructor()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var ctor = results.FirstOrDefault(r => r.MethodName == ".ctor");

        await Assert.That(ctor).IsNull(); // constructors are ConstructorDeclarationSyntax, not MethodDeclarationSyntax
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_empty_list_for_non_existent_directory()
    {
        var nonExistent = Path.Combine(DepthFixturesDir, "does-not-exist");
        var results = DepthDetector.Analyze(nonExistent);

        await Assert.That(results).IsNotNull();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_verify_line_numbers_in_results()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.First(r => r.MethodName == "ShallowHelper");
        await Assert.That(shallow.LineNumber).IsEqualTo(9);
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_suppress_shallow_signal_for_methods_called_from_three_or_more_places()
    {
        // SharedHelper is private, LOC=1, no branching → Phase 1 says shallow(3)
        // but it's called from FirstCaller, SecondCaller, ThirdCaller → Phase 2 suppresses
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shared = results.FirstOrDefault(r => r.MethodName == "SharedHelper");
        await Assert.That(shared).IsNull(); // composite would be 0 after suppression → excluded
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_suppress_shallow_signal_for_single_caller_methods()
    {
        // ShallowHelper is called from one method (Caller) → still flagged
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.FirstOrDefault(r => r.MethodName == "ShallowHelper");
        await Assert.That(shallow).IsNotNull();
        await Assert.That(shallow!.IsShallow).IsTrue();
        await Assert.That(shallow.CompositeScore).IsEqualTo(3);
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task IsWrongAbstraction_should_return_true_when_method_branches_on_formal_parameter()
    {
        var code = """
            class X {
                private string F(string input, string mode) {
                    if (mode == "upper") return input.ToUpper();
                    return input;
                }
            }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);;
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "F");

        await Assert.That(DepthDetector.IsWrongAbstraction(method)).IsTrue();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task IsWrongAbstraction_should_return_false_when_no_branching_on_params()
    {
        var code = """
            class X {
                private string F(string input, string unused) {
                    return input.ToUpper();
                }
            }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);;
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "F");

        await Assert.That(DepthDetector.IsWrongAbstraction(method)).IsFalse();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task GetInvokedMethodName_returns_identifier_for_simple_call()
    {
        var code = """
            class X { void M() { Helper(); } }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);;
        var invoc = root
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First();

        await Assert.That(DepthDetector.GetInvokedMethodName(invoc.Expression)).IsEqualTo("Helper");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task GetInvokedMethodName_returns_member_name_for_qualified_call()
    {
        var code = """
            class X { void M() { obj.DoSomething(); } }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);;
        var invoc = root
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First();

        await Assert.That(DepthDetector.GetInvokedMethodName(invoc.Expression)).IsEqualTo("DoSomething");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task GetInvokedMethodName_returns_null_for_anonymous_delegate_invocation()
    {
        var code = """
            class X { void M() { ((System.Action)(delegate {}))(); } }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);
        var invoc = root
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First();

        // Cast + delegate invocation — not IdentifierName or MemberAccess → returns null
        await Assert.That(DepthDetector.GetInvokedMethodName(invoc.Expression)).IsNull();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Analyze_returns_empty_for_nonexistent_directory()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"depth-nope-{Guid.NewGuid()}");

        var results = DepthDetector.Analyze(nonExistent);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Analyze_returns_empty_for_directory_with_no_csharp_files()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"depth-nocs-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "readme.txt"),
                "not a csharp file").ConfigureAwait(false);

            var results = DepthDetector.Analyze(tempDir);

            await Assert.That(results).IsEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Analyze_detects_shallow_private_method_in_temp_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"depth-cmff-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tempFile = Path.Combine(tempDir, "Test.cs");
            await File.WriteAllTextAsync(tempFile, """
                public class X {
                    private int Helper(int x) { return x + 1; }
                    public void Public() { }
                }
                """).ConfigureAwait(false);

            var results = DepthDetector.Analyze(tempDir);

            // Helper is private, ≤4 lines, no branching, no callers → shallow(3)
            var helper = results.FirstOrDefault(r => r.MethodName == "Helper");
            await Assert.That(helper).IsNotNull();
            await Assert.That(helper!.IsShallow).IsTrue();
            await Assert.That(helper.CompositeScore).IsEqualTo(3);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_flag_property_read_on_parameter_as_entangled()
    {
        // Blocker 1: VisitMemberAccessExpression over-flags pure property reads like
        // items.Count as side effects. A method that reads a property on a parameter
        // is not entangled — it's doing pure data access.
        var code = """
            using System.Collections.Generic;
            class X {
                private int Sum(int a, int b, int c, List<int> items) {
                    return a + b + c + items.Count;
                }
            }
            """;
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(false);
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "Sum");

        var result = DepthDetector.AnalyzeMethod(method, "/test/ShouldNotFlagPropertyRead.cs");

        await Assert.That(result.IsEntangled).IsFalse();
    }
}
