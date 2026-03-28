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
    public async Task All_Generated_Placeholders_Should_Be_Valid_SVG_DataUris()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";
        const string customText = "Custom Text";
        var configuration = new PlaceholderConfiguration();

        // Act
        var defaultResult = scope.Service.GenerateDefaultPlaceholder();
        var reasonResult = scope.Service.GeneratePlaceholderWithReason(reason);
        var customResult = scope.Service.GenerateCustomPlaceholder(customText, configuration);

        // Assert
        await Assert.That(defaultResult).StartsWith("data:image/svg+xml;base64,");
        await Assert.That(reasonResult).StartsWith("data:image/svg+xml;base64,");
        await Assert.That(customResult).StartsWith("data:image/svg+xml;base64,");

        await Assert.That(TestScope.DecodeSvgFromDataUri(defaultResult)).Contains("<svg");
        await Assert.That(TestScope.DecodeSvgFromDataUri(reasonResult)).Contains("<svg");
        await Assert.That(TestScope.DecodeSvgFromDataUri(customResult)).Contains("<svg");
    }

    [Test]
    public async Task GenerateCustomPlaceholder_Should_Be_Consistent_For_Same_Parameters()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "Test";
        var configuration = new PlaceholderConfiguration { Width = 500, Height = 300 };

        // Act
        var result1 = scope.Service.GenerateCustomPlaceholder(customText, configuration);
        var result2 = scope.Service.GenerateCustomPlaceholder(customText, configuration);

        // Assert
        await Assert.That(result1).IsEqualTo(result2);
    }

    [Test]
    public async Task GenerateCustomPlaceholder_Should_Return_Valid_Base64_DataUri()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "Custom Placeholder Text";
        var configuration = new PlaceholderConfiguration();

        // Act
        var result = scope.Service.GenerateCustomPlaceholder(customText, configuration);

        // Assert
        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");
        await Assert.That(result.Length > 50).IsTrue();
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
            TextColor = "#ffffff"
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

    [Test]
    public async Task GenerateDefaultPlaceholder_Should_Return_Valid_Base64_DataUri()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var result = scope.Service.GenerateDefaultPlaceholder();

        // Assert
        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");
        await Assert.That(result.Length > 50).IsTrue();
    }

    [Test]
    public async Task GeneratePlaceholderWithReason_Should_Be_Consistent_For_Same_Reason()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";

        // Act
        var result1 = scope.Service.GeneratePlaceholderWithReason(reason);
        var result2 = scope.Service.GeneratePlaceholderWithReason(reason);

        // Assert
        await Assert.That(result1).IsEqualTo(result2);
    }

    [Test]
    public async Task GeneratePlaceholderWithReason_Should_Return_Valid_Base64_DataUri()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";

        // Act
        var result = scope.Service.GeneratePlaceholderWithReason(reason);

        // Assert
        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");
        await Assert.That(result.Length > 50).IsTrue();
    }
}