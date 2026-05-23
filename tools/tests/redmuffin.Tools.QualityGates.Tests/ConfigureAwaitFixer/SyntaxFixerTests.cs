namespace redmuffin.Tools.QualityGates.Tests.ConfigureAwaitFixer;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.ConfigureAwaitFixer;

public sealed class SyntaxFixerTests
{
    [Test]
    public async Task HasConfigureAwait_ReturnsTrue_WhenConfigureAwaitFalseIsPresent()
    {
        // Arrange
        var code = "await Task.Delay(100).ConfigureAwait(false)";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var awaitExpr = root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HasConfigureAwait_ReturnsFalse_WhenConfigureAwaitIsAbsent()
    {
        // Arrange
        var code = "await Task.Delay(100)";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var awaitExpr = root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HasConfigureAwait_ReturnsFalse_WhenExpressionIsNotAnInvocation()
    {
        // Arrange — 'await task' has IdentifierNameSyntax, not InvocationExpressionSyntax
        var code = """
            using System.Threading.Tasks;
            class C {
                async Task M() {
                    Task t = Task.CompletedTask;
                    await t;
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var awaitExpr = root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task AddConfigureAwait_WrapsWithConfigureAwaitFalse()
    {
        // Arrange
        var code = "await Task.Delay(100)";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var awaitExpr = root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();

        // Act
        var result = SyntaxFixer.AddConfigureAwait(awaitExpr);

        // Assert
        var text = result.ToFullString();
        await Assert.That(text).Contains(".ConfigureAwait(false)");
    }

    [Test]
    public async Task AddConfigureAwait_ReturnsSyntaxThatParses()
    {
        // Arrange
        var code = "await Task.Delay(100)";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var awaitExpr = root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();

        // Act
        var result = SyntaxFixer.AddConfigureAwait(awaitExpr);

        // Assert — the result should be valid C# syntax when wrapped in a statement
        var wrapped = $"async Task M() {{ {result.ToString()}; }}";
        var parsed = CSharpSyntaxTree.ParseText(wrapped);
        var errors = parsed.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(RejectedFilePaths))]
    public async Task IsSourceFile_ReturnsFalse_ForGeneratedAndOutputPaths(string path)
    {
        // Act
        var result = SyntaxFixer.IsSourceFile(path);

        // Assert
        await Assert.That(result).IsFalse();
    }

    public static IEnumerable<string> RejectedFilePaths()
    {
        yield return "/src/Foo.Designer.cs";
        yield return "/src/Bar.g.cs";
        yield return "/src/Baz_AssemblyInfo.cs";
        yield return "/src/obj/Debug/SomeFile.cs";
        yield return "/src/bin/Release/SomeFile.cs";
    }

    [Test]
    [MethodDataSource(nameof(AcceptedFilePaths))]
    public async Task IsSourceFile_ReturnsTrue_ForNormalSourceFiles(string path)
    {
        // Act
        var result = SyntaxFixer.IsSourceFile(path);

        // Assert
        await Assert.That(result).IsTrue();
    }

    public static IEnumerable<string> AcceptedFilePaths()
    {
        yield return "/src/Foo.cs";
        yield return "/src/Services/Bar.cs";
        yield return "/src/Components/Baz.razor.cs";
    }
}
