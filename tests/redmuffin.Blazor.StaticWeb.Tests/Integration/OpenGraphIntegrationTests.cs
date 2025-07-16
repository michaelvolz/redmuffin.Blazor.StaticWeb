using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

public class OpenGraphIntegrationTests : TestBase
{
    private readonly OpenGraphImagesService _imageService;
    private readonly ServiceProvider _serviceProvider;

    public OpenGraphIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
        services.AddSingleton<ICacheService, MockCacheService>();
        services.AddSingleton<IImageValidationService, MockImageValidationService>();
        services.AddSingleton<OpenGraphImagesService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _imageService = _serviceProvider.GetRequiredService<OpenGraphImagesService>();
    }

    [Test]
    public async Task EndToEndImageRetrievalTest()
    {
        // Arrange
        var articleUrls = new List<string>
        {
            "https://example.com/article1",
            "https://example.com/article2"
        };

        // Act
        var images = await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Assert
        await Assert.That(images).IsNotNull();
        await Assert.That(images.Count).IsEqualTo(2);

        // Verify that each URL has a corresponding result
        foreach (var url in articleUrls) await Assert.That(images.ContainsKey(url)).IsTrue();
    }

    [Test]
    public async Task EndToEndImageRetrieval_WithCaching_ShouldUseCacheOnSecondCall()
    {
        // Arrange
        var articleUrls = new List<string>
        {
            "https://example.com/article1"
        };

        // Act - First call should populate cache
        var firstCallImages = await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Act - Second call should use cache
        var secondCallImages = await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Assert
        await Assert.That(firstCallImages).IsNotNull();
        await Assert.That(secondCallImages).IsNotNull();
        await Assert.That(firstCallImages.Count).IsEqualTo(secondCallImages.Count);

        // Verify cached data is returned
        var firstResult = firstCallImages["https://example.com/article1"];
        var secondResult = secondCallImages["https://example.com/article1"];

        await Assert.That(firstResult).IsNotNull();
        await Assert.That(secondResult).IsNotNull();
        await Assert.That(firstResult!.ImageUrl).IsEqualTo(secondResult!.ImageUrl);
    }

    [Test]
    public async Task EndToEndImageRetrieval_WithValidation_ShouldReturnValidatedImages()
    {
        // Arrange
        var articleUrls = new List<string>
        {
            "https://example.com/article-with-image"
        };

        // Act
        var images = await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Assert
        await Assert.That(images).IsNotNull();
        await Assert.That(images.Count).IsEqualTo(1);

        var result = images["https://example.com/article-with-image"];
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsValidated).IsTrue();
        await Assert.That(result.ImageUrl).IsNotNull();
    }

    [Test]
    public async Task EndToEndImageRetrieval_SingleArticle_ShouldReturnImageData()
    {
        // Arrange
        var articleUrl = "https://example.com/single-article";

        // Act
        var image = await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(image).IsNotNull();
        await Assert.That(image!.ArticleUrl).IsEqualTo(articleUrl);
        await Assert.That(image.ImageUrl).IsNotNull();
        await Assert.That(image.IsValidated).IsTrue();
    }

    [Test]
    public async Task EndToEndImageRetrieval_CacheStats_ShouldReturnStatistics()
    {
        // Arrange
        var articleUrls = new List<string>
        {
            "https://example.com/article1",
            "https://example.com/article2"
        };

        // Act - First populate cache
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Act - Get cache stats
        var stats = await _imageService.GetCacheStatsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats.ContainsKey("Namespace")).IsTrue();
        await Assert.That(stats.ContainsKey("TotalItems")).IsTrue();
        await Assert.That(stats["Namespace"]).IsEqualTo("opengraph_images");
    }

    [Test]
    public async Task EndToEndImageRetrieval_CacheInvalidation_ShouldRemoveCachedData()
    {
        // Arrange
        var articleUrl = "https://example.com/article-to-invalidate";

        // Act - First populate cache
        await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);

        // Verify cache contains data
        var isCachedBefore = await _imageService.IsImageCachedAsync(articleUrl).ConfigureAwait(false);

        // Act - Invalidate cache
        var invalidated = await _imageService.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

        // Verify cache is cleared
        var isCachedAfter = await _imageService.IsImageCachedAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(isCachedBefore).IsTrue();
        await Assert.That(invalidated).IsTrue();
        await Assert.That(isCachedAfter).IsFalse();
    }

    [Test]
    public async Task EndToEndImageRetrieval_CacheClearance_ShouldRemoveAllCachedData()
    {
        // Arrange
        var articleUrls = new List<string>
        {
            "https://example.com/article1",
            "https://example.com/article2"
        };

        // Act - First populate cache
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);

        // Act - Clear all cache
        var clearedCount = await _imageService.ClearCacheAsync().ConfigureAwait(false);

        // Verify cache is cleared
        var statsAfterClear = await _imageService.GetCacheStatsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(clearedCount).IsEqualTo(0); // MockCacheService returns 0
        await Assert.That(statsAfterClear).IsNotNull();
        await Assert.That(statsAfterClear["TotalItems"]).IsEqualTo(0);
    }
}

public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope - HttpClient lifecycle is managed by the caller
        return new HttpClient(new TestHttpMessageHandler())
        {
            BaseAddress = new Uri("https://example.com")
        };
#pragma warning restore CA2000
    }
}

public class MockImageValidationService : IImageValidationService
{
    public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new ImageValidationResult
        {
            ImageUrl = imageUrl,
            IsValid = true,
            ContentType = "image/jpeg",
            ContentLength = 1024
        }).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, ImageValidationResult>> ValidateImagesAsync(IEnumerable<string> imageUrls, int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ImageValidationResult>();
        foreach (var url in imageUrls)
            result[url] = new ImageValidationResult
            {
                ImageUrl = url,
                IsValid = true,
                ContentType = "image/jpeg",
                ContentLength = 1024
            };
        return await Task.FromResult(result).ConfigureAwait(false);
    }

    public async Task<ImageValidationResult> ValidateImageWithCacheAsync(string imageUrl, int cacheExpirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        return await ValidateImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
    }

    public Task ClearValidationCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> GetValidationCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new Dictionary<string, object>()).ConfigureAwait(false);
    }
}