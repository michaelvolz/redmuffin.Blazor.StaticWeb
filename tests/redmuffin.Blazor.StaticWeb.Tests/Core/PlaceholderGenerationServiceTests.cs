using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     TUnit tests for PlaceholderGenerationService.
/// </summary>
[Category("Feature:Core")]
[Category("Unit")]
public sealed partial class PlaceholderGenerationServiceTests
{
    [Test]
    [Arguments("default", "No Image Available")]
    [Arguments("reason", "Image not available")]
    [Arguments("custom", "Custom Placeholder Text")]
    public async Task Each_Generator_Entry_Point_Returns_A_Valid_Svg_DataUri_With_Expected_Text(
        string entryPoint,
        string expectedText
    )
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";
        var configuration = new PlaceholderConfiguration();

        // Act
        var result = entryPoint switch
        {
            "default" => scope.Service.GenerateDefaultPlaceholder(),
            "reason" => scope.Service.GeneratePlaceholderWithReason(reason),
            "custom" => scope.Service.GenerateCustomPlaceholder(expectedText, configuration),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint)),
        };

        // Assert
        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");

        var decodedSvg = TestScope.DecodeSvgFromDataUri(result);
        await Assert.That(decodedSvg).Contains("<svg");
        await Assert.That(decodedSvg).Contains(expectedText);
    }

    [Test]
    public async Task GenerateCustomPlaceholder_Should_Use_Custom_Configuration()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "Test";
        var configuration = new PlaceholderConfiguration
        {
            Width = 800,
            Height = 400,
            BackgroundColor = "#ff0000",
            TextColor = "#ffffff",
        };

        // Act
        var result = scope.Service.GenerateCustomPlaceholder(customText, configuration);
        var decodedSvg = TestScope.DecodeSvgFromDataUri(result);

        // Assert
        await Assert.That(decodedSvg).Contains("width=\"800\"");
        await Assert.That(decodedSvg).Contains("height=\"400\"");
        await Assert.That(decodedSvg).Contains("#ff0000");
        await Assert.That(decodedSvg).Contains("#ffffff");
    }

    [Test]
    public async Task GenerateCustomPlaceholder_Should_Use_Custom_Text()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "Custom Placeholder Text";
        var configuration = new PlaceholderConfiguration();

        // Act
        var result = scope.Service.GenerateCustomPlaceholder(customText, configuration);
        var decodedSvg = TestScope.DecodeSvgFromDataUri(result);

        // Assert
        await Assert.That(decodedSvg).Contains(customText);
    }

    [Test]
    public async Task GenerateDefaultPlaceholder_Should_Contain_Default_Text()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var result = scope.Service.GenerateDefaultPlaceholder();
        var decodedSvg = TestScope.DecodeSvgFromDataUri(result);

        // Assert
        await Assert.That(decodedSvg).Contains("No Image Available");
    }
}
