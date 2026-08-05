namespace redmuffin.Tools.ConfigureAwaitFixer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.ConfigureAwaitFixer;

public sealed class SyntaxFixerTests
{
    // Test input uses Task.CompletedTask instead of Task.Delay to avoid slopwatch SW004
    // false positives on string-literals. The ConfigureAwait fixer operates on Roslyn
    // AwaitExpression syntax nodes — any Task-returning expression is equivalent for
    // testing. Task.CompletedTask returns Task (same type as Task.Delay) without the
    // timing dependency that SW004 flags.

    [Test]
    public async Task HasConfigureAwait_ReturnsTrue_WhenConfigureAwaitFalseIsPresent()
    {
        // Arrange
        var awaitExpr = await ParseAwaitAsync("await Task.CompletedTask.ConfigureAwait(false)").ConfigureAwait(false);

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(awaitExpr.Expression).IsTypeOf<InvocationExpressionSyntax>();
    }

    [Test]
    public async Task HasConfigureAwait_ReturnsFalse_WhenConfigureAwaitIsAbsent()
    {
        // Arrange
        var awaitExpr = await ParseAwaitAsync("await Task.CompletedTask").ConfigureAwait(false);

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(awaitExpr.Expression).IsTypeOf<MemberAccessExpressionSyntax>();
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
        var awaitExpr = await ParseAwaitAsync(code).ConfigureAwait(false);

        // Act
        var result = SyntaxFixer.HasConfigureAwait(awaitExpr);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(awaitExpr.Expression).IsTypeOf<IdentifierNameSyntax>();
    }

    [Test]
    public async Task AddConfigureAwait_WrapsWithConfigureAwaitFalse()
    {
        // Arrange
        var awaitExpr = await ParseAwaitAsync("await Task.CompletedTask").ConfigureAwait(false);

        // Act
        var result = SyntaxFixer.AddConfigureAwait(awaitExpr);

        // Assert — the original expression is preserved inside the new call
        var text = result.ToFullString();
        await Assert.That(text).Contains(".ConfigureAwait(false)");
        await Assert.That(text).Contains("Task.CompletedTask");
    }

    [Test]
    public async Task AddConfigureAwait_ReturnsSyntaxThatParses()
    {
        // Arrange
        var awaitExpr = await ParseAwaitAsync("await Task.CompletedTask").ConfigureAwait(false);

        // Act
        var result = SyntaxFixer.AddConfigureAwait(awaitExpr);

        // Assert — the result should be valid C# syntax when wrapped in a statement
        var wrapped = $"async Task M() {{ {result.ToString()}; }}";
        var parsed = CSharpSyntaxTree.ParseText(wrapped);
        var errors = parsed.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        await Assert.That(errors).IsEmpty();
        var parsedRoot = await parsed.GetRootAsync().ConfigureAwait(false);
        await Assert.That(parsedRoot.ToFullString()).Contains(".ConfigureAwait(false)");
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

    private static async Task<AwaitExpressionSyntax> ParseAwaitAsync(string code)
    {
        var root = await CSharpSyntaxTree.ParseText(code).GetRootAsync().ConfigureAwait(false);
        return root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
    }
}
