using LightMock;
using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     Unit tests for OpenGraphImagesService using LightMock.Generator.
///     Migrated from NSubstitute to standardize mocking framework.
///     Focus on null value handling and caching logic.
/// </summary>
public class OpenGraphImagesServiceTestsLightMock : IDisposable
{
    public OpenGraphImagesServiceTestsLightMock()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _imageValidationServiceMock = new Mock<IImageValidationService>();
        _loggerMock = new Mock<ILogger<OpenGraphImagesService>>();

        // Setup default HttpClient mock
        _httpClient = new HttpClient();
        _httpClientFactoryMock.Arrange(f => f.CreateClient()).Returns(() => _httpClient);

        _service = new OpenGraphImagesService(
            _httpClientFactoryMock.Object,
            _cacheServiceMock.Object,
            _imageValidationServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IImageValidationService> _imageValidationServiceMock;
    private readonly Mock<ILogger<OpenGraphImagesService>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly OpenGraphImagesService _service;

    /// <summary>
    ///     Tests CleanupExpiredEntriesAsync calls the cache service.
    /// </summary>
    [Test]
    public async Task CleanupExpiredEntriesAsync_CallsCacheService()
    {
        // Arrange
        var expectedCount = 5;
        _cacheServiceMock.Arrange(f => f.CleanupExpiredItemsAsync("opengraph_images", CancellationToken.None)).Returns(Task.FromResult(expectedCount));

        // Act
        var result = await _service.CleanupExpiredEntriesAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(expectedCount);
        _cacheServiceMock.Assert(f => f.CleanupExpiredItemsAsync("opengraph_images", CancellationToken.None));
    }

    /// <summary>
    ///     Tests ClearCacheAsync calls the cache service.
    /// </summary>
    [Test]
    public async Task ClearCacheAsync_CallsCacheService()
    {
        // Arrange
        _cacheServiceMock.Arrange(f => f.ClearNamespaceAsync("opengraph_images", CancellationToken.None)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ClearCacheAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
        _cacheServiceMock.Assert(f => f.ClearNamespaceAsync("opengraph_images", CancellationToken.None));
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

        _cacheServiceMock.Arrange(f => f.GetNamespaceStatsAsync("opengraph_images", CancellationToken.None))
            .Returns(Task.FromResult(emptyCacheStats));

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

        _cacheServiceMock.Arrange(f => f.GetItemAsync<CachedImageData>("opengraph_images", articleUrl, CancellationToken.None))
            .Returns(Task.FromResult<CachedImageData?>(cachedData));

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
        _cacheServiceMock.Arrange(f => f.GetItemAsync<CachedImageData>("opengraph_images", The<string>.IsAnyValue, CancellationToken.None))
            .Returns(Task.FromResult<CachedImageData?>(null));

        // Act
        var result = await _service.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2); // Only 2 unique valid URLs
        await Assert.That(result.ContainsKey("https://example.com/article1")).IsTrue();
        await Assert.That(result.ContainsKey("https://example.com/article2")).IsTrue();
    }

    /// <summary>
    ///     Tests InvalidateCacheAsync when cache service throws exception.
    /// </summary>
    [Test]
    public async Task InvalidateCacheAsync_WhenCacheServiceThrows_ReturnsFalse()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheServiceMock.Arrange(f => f.RemoveItemAsync("opengraph_images", articleUrl, CancellationToken.None))
            .Returns(() => Task.FromException(new InvalidOperationException("Cache error")));

        // Act
        var result = await _service.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

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
    ///     Tests InvalidateCacheAsync with valid URL.
    /// </summary>
    [Test]
    public async Task InvalidateCacheAsync_WithValidUrl_CallsRemoveAndReturnsTrue()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheServiceMock.Arrange(f => f.RemoveItemAsync("opengraph_images", articleUrl, CancellationToken.None)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
        _cacheServiceMock.Assert(f => f.RemoveItemAsync("opengraph_images", articleUrl, CancellationToken.None));
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

        _cacheServiceMock.Arrange(f => f.SetItemAsync("opengraph_images", articleUrl, imageData, The<int?>.IsAnyValue, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCacheEntryAsync(articleUrl, imageData).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
        _cacheServiceMock.Assert(f => f.SetItemAsync("opengraph_images", articleUrl, imageData, The<int?>.IsAnyValue, CancellationToken.None));
    }
}