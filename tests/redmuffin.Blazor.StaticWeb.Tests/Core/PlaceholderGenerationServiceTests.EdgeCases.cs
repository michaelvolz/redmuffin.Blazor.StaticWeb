using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed partial class PlaceholderGenerationServiceTests
{
    [Test]
    public async Task GenerateCustomPlaceholder_With_Null_Configuration_Should_Throw_ArgumentNullException()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "Test";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(scope.Service.GenerateCustomPlaceholder(customText, null!)));
    }

    [Test]
    public async Task GenerateCustomPlaceholder_With_Null_Text_Should_Throw_ArgumentNullException()
    {
        // Arrange
        using var scope = CreateTestScope();
        var configuration = new PlaceholderConfiguration();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(scope.Service.GenerateCustomPlaceholder(null!, configuration)));
    }

    [Test]
    public async Task GeneratePlaceholderWithReason_With_Null_Reason_Should_Throw_ArgumentNullException()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(scope.Service.GeneratePlaceholderWithReason(null!)));
    }
}