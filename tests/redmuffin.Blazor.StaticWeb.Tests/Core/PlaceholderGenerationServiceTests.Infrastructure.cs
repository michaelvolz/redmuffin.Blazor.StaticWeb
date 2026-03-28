using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public sealed partial class PlaceholderGenerationServiceTests
{
    [Test]
    public async Task Different_Methods_Should_Generate_Different_Placeholders()
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
        await Assert.That(defaultResult).IsNotEqualTo(reasonResult);
        await Assert.That(defaultResult).IsNotEqualTo(customResult);
        await Assert.That(reasonResult).IsNotEqualTo(customResult);
    }

    [Test]
    public async Task GeneratePlaceholderWithReason_Should_Map_Reason_To_Display_Text()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string reason = "LOAD_FAILED";

        // Act
        var result = scope.Service.GeneratePlaceholderWithReason(reason);
        var decodedSvg = TestScope.DecodeSvgFromDataUri(result);

        // Assert
        await Assert.That(decodedSvg).Contains("Image not available");
    }
}