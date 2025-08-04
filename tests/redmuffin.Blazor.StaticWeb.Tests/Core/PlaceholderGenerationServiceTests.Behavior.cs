namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed partial class PlaceholderGenerationServiceTests
{
    [Test]
    public async Task GenerateDefaultPlaceholder_Should_Be_Consistent_Across_Multiple_Calls()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var result1 = scope.Service.GenerateDefaultPlaceholder();
        var result2 = scope.Service.GenerateDefaultPlaceholder();

        // Assert
        await Assert.That(result1).IsEqualTo(result2);
    }
}