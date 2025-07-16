using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;

#pragma warning disable VSTHRD200
#pragma warning disable CA2000 // Dispose objects before losing scope - False positive with NSubstitute

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     Unit tests for OpenGraphImagesService with focus on null value handling and caching logic.
/// </summary>
public class OpenGraphImagesServiceTests : IDisposable
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IImageValidationService _imageValidationService = Substitute.For<IImageValidationService>();
    private readonly ILogger<OpenGraphImagesService> _logger = Substitute.For<ILogger<OpenGraphImagesService>>();
    private readonly OpenGraphImagesService _service;

    public OpenGraphImagesServiceTests()
    {
        // Setup default HttpClient mock
        _httpClient = new HttpClient();
        _httpClientFactory.CreateClient().Returns(_ => _httpClient);

        _service = new OpenGraphImagesService(_httpClientFactory, _cacheService, _imageValidationService, _logger);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     **HIGH PRIORITY TEST** - Tests null value handling in GetCacheStatsAsync method.
    ///     This test specifically addresses the CS8601 nullability issue that was fixed by
    ///     converting nullable DateTime? values to string representations with "N/A" fallback.
    /// </summary>
    [Test]
    public async Task GetCacheStatsAsync_WithEmptyCache_ReturnsStatsWithNullDateHandling()
    {
        // Arrange: Empty cache scenario
        var emptyCacheStats = new CacheNamespaceStats
        {
            Namespace = "opengraph_images",
            TotalItems = 0,
            TotalSizeBytes = 0,
            ExpiredItemsCount = 0,
            OldestItemTimestamp = null,
            NewestItemTimestamp = null,
            AverageAccessCount = 0.0
        };

        _cacheService.GetNamespaceStatsAsync("opengraph_images")
            .Returns(emptyCacheStats);

        // Act: Call the method that handles null DateTime values
        var result = await _service.GetCacheStatsAsync().ConfigureAwait(false);

        // Assert: Verify the structure and null handling
        await Assert.That(result).IsNotNull();
        await Assert.That(result.ContainsKey("Namespace")).IsTrue();
        await Assert.That(result.ContainsKey("TotalItems")).IsTrue();
        await Assert.That(result.ContainsKey("TotalSizeBytes")).IsTrue();
        await Assert.That(result.ContainsKey("ExpiredItemsCount")).IsTrue();
        await Assert.That(result.ContainsKey("OldestItemTimestamp")).IsTrue();
        await Assert.That(result.ContainsKey("NewestItemTimestamp")).IsTrue();
        await Assert.That(result.ContainsKey("AverageAccessCount")).IsTrue();

        // Verify null DateTime handling - should be "N/A" strings, not null
        await Assert.That(result["Namespace"]).IsEqualTo("opengraph_images");
        await Assert.That(result["TotalItems"]).IsEqualTo(0);
        await Assert.That(result["TotalSizeBytes"]).IsEqualTo(0L);
        await Assert.That(result["ExpiredItemsCount"]).IsEqualTo(0);
        await Assert.That(result["OldestItemTimestamp"]).IsEqualTo("N/A");
        await Assert.That(result["NewestItemTimestamp"]).IsEqualTo("N/A");
        await Assert.That(result["AverageAccessCount"]).IsEqualTo(0.0);
    }

    /// <summary>
    ///     Tests IsImageCachedAsync with null/empty URL input.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task IsImageCachedAsync_WithNullOrEmptyUrl_ReturnsFalse(string? articleUrl)
    {
        // Act
        var result = await _service.IsImageCachedAsync(articleUrl!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Tests InvalidateCacheAsync with null/empty URL input.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task InvalidateCacheAsync_WithNullOrEmptyUrl_ReturnsFalse(string? articleUrl)
    {
        // Act
        var result = await _service.InvalidateCacheAsync(articleUrl!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Tests GetImageAsync with null/empty URL input.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task GetImageAsync_WithNullOrEmptyUrl_ReturnsNull(string? articleUrl)
    {
        // Act
        var result = await _service.GetImageAsync(articleUrl!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Tests GetImageAsync with valid URL that exists in cache.
    /// </summary>
    [Test]
    public async Task GetImageAsync_WithCachedImage_ReturnsCachedData()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        var cachedData = new CachedImageData
        {
            ArticleUrl = articleUrl,
            ImageUrl = "https://example.com/image.jpg",
            ImageSource = ImageSource.OpenGraph,
            IsValidated = true,
            CachedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _cacheService.GetItemAsync<CachedImageData>("opengraph_images", articleUrl)
            .Returns(cachedData);

        // Act
        var result = await _service.GetImageAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ArticleUrl).IsEqualTo(articleUrl);
        await Assert.That(result.ImageUrl).IsEqualTo("https://example.com/image.jpg");
        await Assert.That(result.ImageSource).IsEqualTo(ImageSource.OpenGraph);
        await Assert.That(result.IsValidated).IsTrue();
    }

    /// <summary>
    ///     Tests GetImagesAsync with empty URL list.
    /// </summary>
    [Test]
    public async Task GetImagesAsync_WithEmptyUrlList_ReturnsEmptyDictionary()
    {
        // Arrange
        var emptyUrls = new List<string>();

        // Act
        var result = await _service.GetImagesAsync(emptyUrls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Tests GetImagesAsync with mixed valid and invalid URLs.
    /// </summary>
    [Test]
    public async Task GetImagesAsync_WithMixedUrls_FiltersValidUrls()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/article1", "", "https://example.com/article2", "   ", "https://example.com/article1" };

        // Mock cache returns null for all URLs (cache miss)
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        // Act
        var result = await _service.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2); // Only 2 unique valid URLs
        await Assert.That(result.ContainsKey("https://example.com/article1")).IsTrue();
        await Assert.That(result.ContainsKey("https://example.com/article2")).IsTrue();
    }

    /// <summary>
    ///     Tests InvalidateCacheAsync with valid URL.
    /// </summary>
    [Test]
    public async Task InvalidateCacheAsync_WithValidUrl_CallsRemoveAndReturnsTrue()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.RemoveItemAsync("opengraph_images", articleUrl).Returns(Task.CompletedTask);

        // Act
        var result = await _service.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
        await _cacheService.Received(1).RemoveItemAsync("opengraph_images", articleUrl).ConfigureAwait(false);
    }

    /// <summary>
    ///     Tests InvalidateCacheAsync when cache service throws exception.
    /// </summary>
    [Test]
    public async Task InvalidateCacheAsync_WhenCacheServiceThrows_ReturnsFalse()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.RemoveItemAsync("opengraph_images", articleUrl)
            .Throws(new InvalidOperationException("Cache error"));

        // Act
        var result = await _service.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Tests ClearCacheAsync calls the cache service.
    /// </summary>
    [Test]
    public async Task ClearCacheAsync_CallsCacheService()
    {
        // Arrange
        _cacheService.ClearNamespaceAsync("opengraph_images").Returns(Task.CompletedTask);

        // Act
        var result = await _service.ClearCacheAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await _cacheService.Received(1).ClearNamespaceAsync("opengraph_images").ConfigureAwait(false);
    }

    /// <summary>
    ///     Tests CleanupExpiredEntriesAsync calls the cache service.
    /// </summary>
    [Test]
    public async Task CleanupExpiredEntriesAsync_CallsCacheService()
    {
        // Arrange
        var expectedCount = 5;
        _cacheService.CleanupExpiredItemsAsync("opengraph_images").Returns(expectedCount);

        // Act
        var result = await _service.CleanupExpiredEntriesAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(expectedCount);
        await _cacheService.Received(1).CleanupExpiredItemsAsync("opengraph_images").ConfigureAwait(false);
    }

    /// <summary>
    ///     Tests UpdateCacheEntryAsync with null parameters.
    /// </summary>
    [Test]
    [Arguments(null, true)] // null articleUrl, valid imageData
    [Arguments("https://example.com/article", false)] // valid articleUrl, null imageData
    [Arguments(null, false)] // both null
    [Arguments("", true)] // empty articleUrl, valid imageData
    [Arguments("   ", true)] // whitespace articleUrl, valid imageData
    public async Task UpdateCacheEntryAsync_WithInvalidParameters_ReturnsFalse(string? articleUrl, bool hasImageData)
    {
        // Arrange
        var imageData = hasImageData
            ? new CachedImageData
            {
                ArticleUrl = "https://example.com/article",
                ImageUrl = "https://example.com/image.jpg",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
            : null;

        // Act
        var result = await _service.UpdateCacheEntryAsync(articleUrl!, imageData!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Tests UpdateCacheEntryAsync with valid parameters.
    /// </summary>
    [Test]
    public async Task UpdateCacheEntryAsync_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        var imageData = new CachedImageData
        {
            ArticleUrl = articleUrl,
            ImageUrl = "https://example.com/image.jpg",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _cacheService.SetItemAsync("opengraph_images", articleUrl, imageData, Arg.Any<int>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCacheEntryAsync(articleUrl, imageData).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
        await _cacheService.Received(1).SetItemAsync("opengraph_images", articleUrl, imageData, Arg.Any<int>()).ConfigureAwait(false);
    }
}