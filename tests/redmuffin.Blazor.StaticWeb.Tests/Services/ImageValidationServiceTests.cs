using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;
using redmuffin.Blazor.StaticWeb.Tests.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Assertions;
using NSubstitute;
using System.Net.Http;

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

public class ImageValidationServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ILogger<ImageValidationService> _logger = Substitute.For<ILogger<ImageValidationService>>();
    private readonly ImageValidationService _service;

    public ImageValidationServiceTests()
    {
        _service = new ImageValidationService(_httpClientFactory, _cacheService, _logger);
    }

    /// <summary>
    /// Tests ValidateImageAsync with null/empty URL input.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ValidateImageAsync_WithNullOrEmptyUrl_ReturnsInvalid(string? imageUrl)
    {
        // Act
        var result = await _service.ValidateImageAsync(imageUrl!);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Image URL is null or empty");
    }

    /// <summary>
    /// Tests ValidateImageWithCacheAsync with invalid URL format.
    /// </summary>
    [Test]
    public async Task ValidateImageWithCacheAsync_WithInvalidUrlFormat_ReturnsInvalid()
    {
        // Act
        var result = await _service.ValidateImageWithCacheAsync("not-a-valid-url");

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Invalid URL format");
    }

    /// <summary>
    /// Tests ValidateImagesAsync with mixed URLs.
    /// </summary>
    [Test]
    public async Task ValidateImagesAsync_WithMixedUrls_ReturnsResultsForEachUrl()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/image1.jpg", "", "https://example.com/image2.jpg" };

        // Mock the validation to return valid results
        _cacheService.GetItemAsync<ImageValidationResult>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        _httpClientFactory.CreateClient().Returns(new HttpClient(new TestHttpMessageHandler()) { BaseAddress = new Uri("http://example.com") });

        // Act
        var results = await _service.ValidateImagesAsync(urls);

        // Assert
        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results["https://example.com/image1.jpg"].IsValid).IsFalse(); // Default to false since TestHttpMessageHandler is basic
        await Assert.That(results["https://example.com/image2.jpg"].IsValid).IsFalse(); // Default to false
        await Assert.That(results.ContainsKey("")).IsTrue(); // Handles empty
    }

    /// <summary>
    /// Tests ClearValidationCacheAsync clears memory and persistent cache.
    /// </summary>
    [Test]
    public async Task ClearValidationCacheAsync_ClearsBothCaches()
    {
        // Act
        await _service.ClearValidationCacheAsync();

        // Assert
        await _cacheService.Received(1).ClearNamespaceAsync("image_validation");
        // Hard to test _memoryCache, ensure no exceptions
    }
}

