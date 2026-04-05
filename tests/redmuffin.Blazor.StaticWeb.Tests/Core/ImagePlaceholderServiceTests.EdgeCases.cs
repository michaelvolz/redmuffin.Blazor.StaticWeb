using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public sealed partial class ImagePlaceholderServiceTests
{
    [Test]
    public async Task GetFallbackReason_WithFailedCache_ShouldReturnLoadFailedReason()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link ?? testItem.Id.ToString());

        // Act
        var result = service.GetFallbackReason(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsEqualTo("Image not available");
    }

    [Test]
    public async Task GetFallbackReason_WithNullCover_ShouldReturnNoImageReason()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var itemWithNullCover = CreateTestItem(cover: null);
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.GetFallbackReason(itemWithNullCover, imageUrlCache);

        // Assert
        await Assert.That(result).IsEqualTo("Image not available");
    }

    [Test]
    public async Task GetFallbackReason_WithValidImage_ShouldReturnEmptyString()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.GetFallbackReason(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetImageUrl_WithCachedFailedUrl_ShouldReturnPlaceholder()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link ?? testItem.Id.ToString());

        // Act
        var result = service.GetImageUrl(testItem, imageUrlCache);

        // Assert
        await AssertIsSvgDataUrl(result).ConfigureAwait(false);
    }

    [Test]
    public async Task GetImageUrl_WithNullCover_ShouldReturnDefaultPlaceholder()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var itemWithNullCover = CreateTestItem(cover: null);
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.GetImageUrl(itemWithNullCover, imageUrlCache);

        // Assert
        await AssertIsSvgDataUrl(result).ConfigureAwait(false);
    }

    [Test]
    public async Task HandleImageLoadAsync_WithFailedLoad_ShouldCacheFailureAndStopShimmer()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
        var elementId = "test-element";
        var itemLink = "https://example.com/test";
        var imageUrlCache = new Dictionary<string, string>();
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
            false,
            imageUrlCache,
            jsRuntime,
            StateChangedCallback).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache[itemLink]).IsEqualTo("FAILED");
        await Assert.That(stateChangedCalled).IsTrue();
    }

    [Test]
    public async Task HasFallbackPlaceholder_WithFailedCache_ShouldReturnTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link ?? testItem.Id.ToString());

        // Act
        var result = service.HasFallbackPlaceholder(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HasFallbackPlaceholder_WithNullCover_ShouldReturnTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var itemWithNullCover = CreateTestItem(cover: null);
        var imageUrlCache = new Dictionary<string, string>();

        // Act
        var result = service.HasFallbackPlaceholder(itemWithNullCover, imageUrlCache);

        // Assert
        await Assert.That(result).IsTrue();
    }
}