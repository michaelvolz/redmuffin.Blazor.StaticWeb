namespace redmuffin.Blazor.StaticWeb.Tests.CodeQuality;

/// <summary>
///     Validates architectural standards for Blazor components using behavior-focused testing.
///     Enforces code-behind patterns and separation of concerns as defined in project guidelines.
/// </summary>
[Category("Feature:CodeQuality")]
[Category("Unit")]
public sealed partial class BlazorCodeBehindEnforcementTests
{
    private static readonly string ProjectRoot = GetProjectRoot();
    private static readonly string BlazorProjectPath = Path.Combine(ProjectRoot, "src", "redmuffin.Blazor.StaticWeb");


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
}