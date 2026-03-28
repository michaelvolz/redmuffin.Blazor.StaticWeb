namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

/// <summary>
///     Infrastructure tests for Blazor code-behind enforcement.
/// </summary>
[Category("Feature:CodeQuality")]
public sealed partial class BlazorCodeBehindEnforcementTests
{
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