using System.Text.RegularExpressions;

namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

/// <summary>
///     Code quality tests that enforce architectural standards for Blazor components.
///     These tests ensure adherence to the code-behind preference established in project guidelines.
/// </summary>
public partial class BlazorCodeBehindEnforcementTests
{
    private static readonly string ProjectRoot = GetProjectRoot();
    private static readonly string BlazorProjectPath = Path.Combine(ProjectRoot, "src", "redmuffin.Blazor.StaticWeb");

    /// <summary>
    ///     Validates that existing code-behind files follow proper naming conventions.
    ///     Ensures consistency in the codebase file organization.
    /// </summary>
    [Test]
    public async Task Should_FollowNamingConventions_ForCodeBehindFiles()
    {
        // Arrange
        var codeBehindFiles = GetAllCodeBehindFiles();
        var invalidNamingFiles = new List<string>();

        // Act
        foreach (var codeBehindFile in codeBehindFiles)
        {
            var expectedRazorFile = codeBehindFile.Substring(0, codeBehindFile.Length - 3); // Remove ".cs"

            if (!File.Exists(expectedRazorFile)) invalidNamingFiles.Add(GetRelativePath(codeBehindFile));
        }

        // Assert
        if (invalidNamingFiles.Count > 0)
            Assert.Fail($"Found .razor.cs files without corresponding .razor files. " +
                        $"Check naming conventions for: {string.Join(", ", invalidNamingFiles)}");

        await Assert.That(invalidNamingFiles).IsEmpty();
    }

    /// <summary>
    ///     Verifies that components with complex logic have corresponding .razor.cs code-behind files.
    ///     This test helps identify components that might benefit from code-behind separation.
    /// </summary>
    [Test]
    public async Task Should_HaveCodeBehindFiles_ForComplexComponents()
    {
        // Arrange
        var razorFiles = GetAllRazorFiles();
        var componentsWithoutCodeBehind = new List<string>();
        var complexityIndicators = new[]
        {
            "@inject",
            "@implements",
            "[Parameter]",
            "OnInitialized",
            "OnParametersSet",
            "OnAfterRender"
        };

        // Act
        foreach (var razorFile in razorFiles)
        {
            var content = await File.ReadAllTextAsync(razorFile).ConfigureAwait(false);
            var codeBehindFile = razorFile + ".cs";

            // Check if file has complexity indicators but no code-behind
            var hasComplexity = complexityIndicators.Any(indicator =>
                content.Contains(indicator, StringComparison.OrdinalIgnoreCase));

            if (hasComplexity && !File.Exists(codeBehindFile)) componentsWithoutCodeBehind.Add(GetRelativePath(razorFile));
        }

        // Assert
        if (componentsWithoutCodeBehind.Count > 0)
            Assert.Fail($"Found complex components without code-behind files. " +
                        $"Consider creating .razor.cs files for: {string.Join(", ", componentsWithoutCodeBehind)}");

        await Assert.That(componentsWithoutCodeBehind).IsEmpty();
    }

    /// <summary>
    ///     Enforces that all .razor files use code-behind instead of inline @code blocks.
    ///     This test supports the project's architectural decision to separate concerns and improve maintainability.
    /// </summary>
    [Test]
    public async Task Should_NotContain_InlineCodeBlocks_InRazorFiles()
    {
        // Arrange
        var razorFiles = GetAllRazorFiles();
        var filesWithInlineCode = new List<string>();
        var codeBlockPattern = new Regex(@"@code\s*\{", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Act
        foreach (var razorFile in razorFiles)
        {
            var content = await File.ReadAllTextAsync(razorFile).ConfigureAwait(false);

            if (codeBlockPattern.IsMatch(content)) filesWithInlineCode.Add(GetRelativePath(razorFile));
        }

        // Assert
        if (filesWithInlineCode.Count > 0)
            Assert.Fail($"Found inline @code blocks in the following .razor files. " +
                        $"Please move code to corresponding .razor.cs files: {string.Join(", ", filesWithInlineCode)}");

        await Assert.That(filesWithInlineCode).IsEmpty();
    }
}