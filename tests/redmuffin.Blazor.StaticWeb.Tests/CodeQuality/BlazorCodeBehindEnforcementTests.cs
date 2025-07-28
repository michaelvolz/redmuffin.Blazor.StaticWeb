using System.Text.RegularExpressions;

namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

/// <summary>
///     Validates architectural standards for Blazor components using behavior-focused testing.
///     Enforces code-behind patterns and separation of concerns as defined in project guidelines.
/// </summary>
public sealed partial class BlazorCodeBehindEnforcementTests
{
    private static readonly string ProjectRoot = GetProjectRoot();
    private static readonly string BlazorProjectPath = Path.Combine(ProjectRoot, "src", "redmuffin.Blazor.StaticWeb");

    /// <summary>
    ///     Validates that Razor files maintain clean markup by avoiding inline code blocks.
    /// </summary>
    [Test]
    public async Task Should_Avoid_Inline_Code_Blocks_When_Razor_Files_Contain_Logic()
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

    /// <summary>
    ///     Validates that code-behind files maintain proper naming conventions and file relationships.
    /// </summary>
    [Test]
    public async Task Should_Maintain_Proper_Naming_Conventions_When_Code_Behind_Files_Exist()
    {
        // Arrange
        var codeBehindFiles = GetAllCodeBehindFiles();
        var invalidNamingFiles = new List<string>();

        // Act
        foreach (var codeBehindFile in codeBehindFiles)
        {
            var expectedRazorFile = codeBehindFile[..^3]; // Remove ".cs"

            if (!File.Exists(expectedRazorFile)) invalidNamingFiles.Add(GetRelativePath(codeBehindFile));
        }

        // Assert
        if (invalidNamingFiles.Count > 0)
            Assert.Fail($"Found .razor.cs files without corresponding .razor files. " +
                        $"Check naming conventions for: {string.Join(", ", invalidNamingFiles)}");

        await Assert.That(invalidNamingFiles).IsEmpty();
    }

    /// <summary>
    ///     Validates that complex components utilize code-behind files for proper separation of concerns.
    /// </summary>
    [Test]
    public async Task Should_Use_Code_Behind_Files_When_Components_Have_Complex_Logic()
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
}