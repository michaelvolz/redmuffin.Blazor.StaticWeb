using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public sealed partial class PlaceholderGenerationServiceTests
{
    [Test]
    [Arguments("default")]
    [Arguments("reason")]
    [Arguments("custom")]
    public async Task Repeated_Calls_To_Each_Generator_Entry_Point_Return_Equal_Output(
        string entryPoint
    )
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";
        const string customText = "Test";
        var configuration = new PlaceholderConfiguration { Width = 500, Height = 300 };

        // Act
        var (first, second) = entryPoint switch
        {
            "default" => (
                scope.Service.GenerateDefaultPlaceholder(),
                scope.Service.GenerateDefaultPlaceholder()
            ),
            "reason" => (
                scope.Service.GeneratePlaceholderWithReason(reason),
                scope.Service.GeneratePlaceholderWithReason(reason)
            ),
            "custom" => (
                scope.Service.GenerateCustomPlaceholder(customText, configuration),
                scope.Service.GenerateCustomPlaceholder(customText, configuration)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint)),
        };

        // Assert
        await Assert.That(first).IsEqualTo(second);
    }
}
