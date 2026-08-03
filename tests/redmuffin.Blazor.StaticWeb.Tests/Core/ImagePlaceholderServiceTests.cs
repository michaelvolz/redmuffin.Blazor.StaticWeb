using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
[Category("Unit")]
public partial class ImagePlaceholderServiceTests
{
    [Test]
    public async Task GetDefaultPlaceholder_ShouldReturnValidSvg()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();

        // Act
        var result = service.GetDefaultPlaceholder();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.StartsWith("data:image/svg+xml;base64,")).IsTrue();
        await Assert.That(result.Length > 50).IsTrue();
    }


    [Test]
    public async Task GetImageUrl_WithValidCover_ShouldReturnCoverUrl()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.GetImageUrl(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsEqualTo(testItem.Cover);
    }


    [Test]
    public async Task HandleImageLoadAsync_WithSuccessfulLoad_ShouldStopShimmer()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var elementId = "test-element";
        var itemLink = "https://example.com/test";
        var imageUrlCache = new Dictionary<string, string>();
        var stopShimmerAsync = static (string _) => Task.CompletedTask;
        var stateChangedCalled = false;

        Task StateChangedCallback()
        {
            stateChangedCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await service.HandleImageLoadAsync(
            elementId,
            itemLink,
            true,
            imageUrlCache,
            stopShimmerAsync,
            StateChangedCallback).ConfigureAwait(false);

        // Assert
        await Assert.That(stateChangedCalled).IsTrue();
    }


    [Test]
    public async Task HasFallbackPlaceholder_WithValidCover_ShouldReturnFalse()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.HasFallbackPlaceholder(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsFalse();
    }
}