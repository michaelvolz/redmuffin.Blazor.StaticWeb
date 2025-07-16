using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;
using redmuffin.Blazor.StaticWeb.Tests.Integration;

namespace redmuffin.Blazor.StaticWeb.Tests.ErrorScenarios;

public class OpenGraphErrorScenarioTests : TestBase
{
    private readonly ICacheService _cacheService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenGraphImagesService _imageService;
    private readonly IImageValidationService _imageValidationService;
    private readonly ServiceProvider _serviceProvider;

    public OpenGraphErrorScenarioTests()
    {
        var services = new ServiceCollection();

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _cacheService = Substitute.For<ICacheService>();
        _imageValidationService = Substitute.For<IImageValidationService>();

        services.AddSingleton(_httpClientFactory);
        services.AddSingleton(_cacheService);
        services.AddSingleton(_imageValidationService);
        services.AddSingleton<OpenGraphImagesService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _imageService = _serviceProvider.GetRequiredService<OpenGraphImagesService>();
    }

    [Test]
    public async Task GetImageAsync_WithNullUrl_ShouldReturnNull()
    {
        // Act
        var result = await _imageService.GetImageAsync(null!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetImageAsync_WithEmptyUrl_ShouldReturnNull()
    {
        // Act
        var result = await _imageService.GetImageAsync("").ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetImageAsync_WithWhitespaceUrl_ShouldReturnNull()
    {
        // Act
        var result = await _imageService.GetImageAsync("   ").ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetImageAsync_WhenApiCallFails_ShouldReturnNull()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new FailingHttpMessageHandler(HttpStatusCode.InternalServerError));
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Act
        var result = await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetImageAsync_WhenApiCallTimesOut_ShouldReturnNull()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new TimeoutHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Act
        var result = await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetImageAsync_WhenCacheServiceThrows_ShouldHandleGracefully()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache service error"));

        var httpClient = new HttpClient(new SuccessHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Act & Assert - Should not throw exception, but may return null due to cache write failure
        var result = await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);
        // The service should handle the error gracefully and not crash
        // Result may be null if both cache read and write operations fail
        await Assert.That(result).IsNull(); // Cache failures prevent successful operation
    }

    [Test]
    public async Task GetImagesAsync_WithEmptyList_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = await _imageService.GetImagesAsync(new List<string>()).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetImagesAsync_WithNullList_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = await _imageService.GetImagesAsync(null!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetImagesAsync_WithMixedValidAndInvalidUrls_ShouldFilterInvalidUrls()
    {
        // Arrange
        var urls = new List<string> { "https://valid.com", "", "https://valid2.com", null!, "   " };
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new SuccessHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Act
        var result = await _imageService.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2); // Only valid URLs should be processed
        await Assert.That(result.ContainsKey("https://valid.com")).IsTrue();
        await Assert.That(result.ContainsKey("https://valid2.com")).IsTrue();
    }

    [Test]
    public async Task GetImagesAsync_WhenImageValidationFails_ShouldReturnNullForFailedUrls()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/article1", "https://example.com/article2" };
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new SuccessHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Setup image validation to fail
        _imageValidationService.ValidateImagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ImageValidationResult>
            {
                ["https://example.com/image1.jpg"] = new() { IsValid = false, ErrorMessage = "Image not found" },
                ["https://example.com/image2.jpg"] = new() { IsValid = false, ErrorMessage = "Image not accessible" }
            });

        // Act
        var result = await _imageService.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.Values.All(v => v == null)).IsTrue(); // All should be null due to validation failure
    }

    [Test]
    public async Task IsImageCachedAsync_WithInvalidUrl_ShouldReturnFalse()
    {
        // Act & Assert
        await Assert.That(await _imageService.IsImageCachedAsync(null!).ConfigureAwait(false)).IsFalse();
        await Assert.That(await _imageService.IsImageCachedAsync("").ConfigureAwait(false)).IsFalse();
        await Assert.That(await _imageService.IsImageCachedAsync("   ").ConfigureAwait(false)).IsFalse();
    }

    [Test]
    public async Task InvalidateCacheAsync_WithInvalidUrl_ShouldReturnFalse()
    {
        // Act & Assert
        await Assert.That(await _imageService.InvalidateCacheAsync(null!).ConfigureAwait(false)).IsFalse();
        await Assert.That(await _imageService.InvalidateCacheAsync("").ConfigureAwait(false)).IsFalse();
        await Assert.That(await _imageService.InvalidateCacheAsync("   ").ConfigureAwait(false)).IsFalse();
    }

    [Test]
    public async Task InvalidateCacheAsync_WhenCacheServiceThrows_ShouldReturnFalse()
    {
        // Arrange
        var articleUrl = "https://example.com/article";
        _cacheService.RemoveItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache service error"));

        // Act
        var result = await _imageService.InvalidateCacheAsync(articleUrl).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetCacheStatsAsync_WhenCacheServiceThrows_ShouldReturnEmptyDictionary()
    {
        // Arrange
        _cacheService.GetNamespaceStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache service error"));

        // Act
        var result = await _imageService.GetCacheStatsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClearCacheAsync_WhenCacheServiceThrows_ShouldReturnZero()
    {
        // Arrange
        _cacheService.ClearNamespaceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache service error"));

        // Act
        var result = await _imageService.ClearCacheAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task CleanupExpiredEntriesAsync_WhenCacheServiceThrows_ShouldReturnZero()
    {
        // Arrange
        _cacheService.CleanupExpiredItemsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache service error"));

        // Act
        var result = await _imageService.CleanupExpiredEntriesAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task UpdateCacheEntryAsync_WithNullData_ShouldReturnFalse()
    {
        // Act
        var result = await _imageService.UpdateCacheEntryAsync("https://example.com", null!).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UpdateCacheEntryAsync_WithExpiredData_ShouldReturnTrue()
    {
        // Arrange
        var expiredData = new CachedImageData
        {
            ArticleUrl = "https://example.com",
            ImageUrl = "https://example.com/image.jpg",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        // Act
        var result = await _imageService.UpdateCacheEntryAsync("https://example.com", expiredData).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue(); // Should still return true, but won't actually cache
    }

    [Test]
    public async Task GetImagesAsync_WhenAllArticlesFailValidation_ShouldReturnNullValues()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/article1", "https://example.com/article2" };
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new PartialFailureHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Setup image validation to fail for all images
        _imageValidationService.ValidateImagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ImageValidationResult>
            {
                ["https://example.com/image1.jpg"] = new() { IsValid = false, ErrorMessage = "404 Not Found" },
                ["https://example.com/image2.jpg"] = new() { IsValid = false, ErrorMessage = "403 Forbidden" }
            });

        // Act
        var result = await _imageService.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.Values.All(v => v == null)).IsTrue();
    }

    [Test]
    public async Task GetImagesAsync_WhenImageValidationServiceThrows_ShouldHandleGracefully()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/article1" };
        _cacheService.GetItemAsync<CachedImageData>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CachedImageData?)null);

        var httpClient = new HttpClient(new SuccessHttpMessageHandler());
        _httpClientFactory.CreateClient().Returns(httpClient);

        // Setup image validation to throw exception
        _imageValidationService.ValidateImagesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network error"));

        // Act
        var result = await _imageService.GetImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Values.All(v => v == null)).IsTrue(); // Should return null due to validation failure
    }
}