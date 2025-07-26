using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Core;

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
    public async Task GetImageUrl_WithCachedFailedUrl_ShouldReturnPlaceholder()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link);

        // Act
        var result = service.GetImageUrl(testItem, imageUrlCache);

        // Assert
        await AssertIsSvgDataUrl(result).ConfigureAwait(false);
    }

    [Test]
    public async Task GetImageUrl_WithCachedValidUrl_ShouldReturnCachedUrl()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var cachedUrl = "https://cached.example.com/image.jpg";
        var imageUrlCache = CreateCacheWithValidItem(testItem.Link, cachedUrl);

        // Act
        var result = service.GetImageUrl(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsEqualTo(cachedUrl);
    }

    [Test]
    public async Task HandleImageLoadAsync_WithSuccessfulLoad_ShouldStopShimmer()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
        var elementId = "test-element";
        var itemLink = "https://example.com/test";
        var imageUrlCache = new Dictionary<string, string>();
        var stateChangedCalled = false;
        Func<Task> stateChangedCallback = () => { stateChangedCalled = true; return Task.CompletedTask; };

        // Act
        await service.HandleImageLoadAsync(
            elementId,
            itemLink,
            true,
            imageUrlCache,
            jsRuntime,
            stateChangedCallback).ConfigureAwait(false);

        // Assert
        await Assert.That(stateChangedCalled).IsTrue();
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
        Func<Task> stateChangedCallback = () => { stateChangedCalled = true; return Task.CompletedTask; };

        // Act
        await service.HandleImageLoadAsync(
            elementId,
            itemLink,
            false,
            imageUrlCache,
            jsRuntime,
            stateChangedCallback).ConfigureAwait(false);

        // Assert
        await Assert.That(imageUrlCache[itemLink]).IsEqualTo("FAILED");
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

    [Test]
    public async Task HasFallbackPlaceholder_WithFailedCache_ShouldReturnTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link);

        // Act
        var result = service.HasFallbackPlaceholder(testItem, imageUrlCache);

        // Assert
        await Assert.That(result).IsTrue();
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
    public async Task GetFallbackReason_WithFailedCache_ShouldReturnLoadFailedReason()
    {
        // Arrange
        using var scope = CreateTestScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagePlaceholderService>();
        var testItem = CreateTestItem();
        var imageUrlCache = CreateCacheWithFailedItem(testItem.Link);

        // Act
        var result = service.GetFallbackReason(testItem, imageUrlCache);

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
}