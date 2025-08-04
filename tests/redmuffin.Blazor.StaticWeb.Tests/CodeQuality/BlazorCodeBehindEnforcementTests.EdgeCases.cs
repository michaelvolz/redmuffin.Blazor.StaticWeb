using System.Text.RegularExpressions;

namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

public sealed partial class BlazorCodeBehindEnforcementTests
{
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
}