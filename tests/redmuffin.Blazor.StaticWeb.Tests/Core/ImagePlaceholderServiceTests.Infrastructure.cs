using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed partial class ImagePlaceholderServiceTests
{
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
}