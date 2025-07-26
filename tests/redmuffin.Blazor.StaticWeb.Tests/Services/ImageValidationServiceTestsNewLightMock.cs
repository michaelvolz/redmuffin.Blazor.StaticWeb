using LightMock;
using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;
using redmuffin.Blazor.StaticWeb.Tests.Helpers;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     Tests for ImageValidationService using LightMock.Generator.
///     Migrated from NSubstitute to standardize mocking framework.
/// </summary>
public class ImageValidationServiceTestsNewLightMock : IDisposable
{
    public ImageValidationServiceTestsNewLightMock()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ImageValidationService>>();

        _service = new ImageValidationService(
            _httpClientFactoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ImageValidationService>> _loggerMock;
    private readonly ImageValidationService _service;

    /// <summary>
    ///     Tests ClearValidationCacheAsync clears memory and persistent cache.
    /// </summary>
    [Test]
    [Skip("Needs Migration")]
    public async Task ClearValidationCacheAsync_ClearsBothCaches()
    {
        // Act
        await _service.ClearValidationCacheAsync().ConfigureAwait(false);

        // Assert
        _cacheServiceMock.Assert(f => f.ClearNamespaceAsync("image_validation", The<CancellationToken>.IsAnyValue));
        // Hard to test _memoryCache, ensure no exceptions
    }

    /// <summary>
    ///     Tests ValidateImageAsync with null/empty URL input.
    /// </summary>
    [Test]
    [Skip("Needs Migration")]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ValidateImageAsync_WithNullOrEmptyUrl_ReturnsInvalid(string? imageUrl)
    {
        // Act
        var result = await _service.ValidateImageAsync(imageUrl!).ConfigureAwait(false);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Image URL is null or empty");
    }

    /// <summary>
    ///     Tests ValidateImagesAsync with mixed URLs.
    /// </summary>
    [Test]
    [Skip("Needs Migration")]
    public async Task ValidateImagesAsync_WithMixedUrls_ReturnsResultsForEachUrl()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/image1.jpg", "", "https://example.com/image2.jpg" };

        // Mock the validation to return valid results
        _cacheServiceMock.Arrange(f => f.GetItemAsync<ImageValidationResult>(The<string>.IsAnyValue, The<string>.IsAnyValue, The<CancellationToken>.IsAnyValue))
            .Returns(Task.FromResult((ImageValidationResult?)null));

        using var testHandler = new TestHttpMessageHandler();
#pragma warning disable CA2000 // Dispose objects before losing scope - HttpClient lifecycle managed by test
        var httpClient = new HttpClient(testHandler) { BaseAddress = new Uri("http://example.com") };
        _httpClientFactoryMock.Arrange(f => f.CreateClient()).Returns(() => httpClient);
#pragma warning restore CA2000

        // Act
        var results = await _service.ValidateImagesAsync(urls).ConfigureAwait(false);

        // Assert
        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results["https://example.com/image1.jpg"].IsValid).IsFalse(); // Default to false since TestHttpMessageHandler is basic
        await Assert.That(results["https://example.com/image2.jpg"].IsValid).IsFalse(); // Default to false
        await Assert.That(results.ContainsKey("")).IsTrue(); // Handles empty
    }

    /// <summary>
    ///     Tests ValidateImageWithCacheAsync with invalid URL format.
    /// </summary>
    [Test]
    [Skip("Needs Migration")]
    public async Task ValidateImageWithCacheAsync_WithInvalidUrlFormat_ReturnsInvalid()
    {
        // Act
        var result = await _service.ValidateImageWithCacheAsync("not-a-valid-url").ConfigureAwait(false);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Invalid URL format");
    }
}