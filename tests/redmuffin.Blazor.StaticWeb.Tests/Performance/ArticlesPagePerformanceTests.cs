using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Performance;

/// <summary>
///     Performance tests for the Articles page to validate the image delay bug fix.
///     Ensures articles render within 500ms and background validation works correctly.
/// </summary>
public sealed class ArticlesPagePerformanceTests : IDisposable
{
    public ArticlesPagePerformanceTests()
    {
        // Setup mock dependencies
        _mockImageValidationService = Substitute.For<IImageValidationService>();
        _mockOpenGraphImagesService = Substitute.For<IOpenGraphImagesService>();
        _mockLogger = Substitute.For<ILogger<Articles>>();
        _mockJsRuntime = Substitute.For<IJSRuntime>();
        _mockHttpClient = Substitute.For<HttpClient>();

        // Create component instance
        _articlesComponent = new Articles();

        // Set up dependencies using reflection
        SetPrivateProperty(_articlesComponent, "ImageValidationService", _mockImageValidationService);
        SetPrivateProperty(_articlesComponent, "OpenGraphImagesService", _mockOpenGraphImagesService);
        SetPrivateProperty(_articlesComponent, "Logger", _mockLogger);
        SetPrivateProperty(_articlesComponent, "Js", _mockJsRuntime);
        SetPrivateProperty(_articlesComponent, "Navigation", new MockNavigationManager());
        SetPrivateProperty(_articlesComponent, "Http", _mockHttpClient);

        // Get the validation semaphore for testing
        _validationSemaphore = GetPrivateField<SemaphoreSlim>(_articlesComponent, "_validationSemaphore");
    }

    public void Dispose()
    {
        _articlesComponent?.Dispose();
        _validationSemaphore?.Dispose();
    }

    private readonly Articles _articlesComponent;
    private readonly IImageValidationService _mockImageValidationService;
    private readonly IOpenGraphImagesService _mockOpenGraphImagesService;
    private readonly ILogger<Articles> _mockLogger;
    private readonly IJSRuntime _mockJsRuntime;
    private readonly HttpClient _mockHttpClient;
    private readonly SemaphoreSlim _validationSemaphore;

    // Helper methods
    private static List<RaindropItem> CreateTestArticles(int count)
    {
        var articles = new List<RaindropItem>();
        for (var i = 1; i <= count; i++)
            articles.Add(new RaindropItem
            {
                Id = i,
                Link = $"https://example.com/article{i}",
                Cover = $"https://example.com/image{i}.jpg",
                Title = $"Test Article {i}",
                Excerpt = $"This is test article {i}",
                Created = DateTime.UtcNow.AddDays(-i)
            });
        return articles;
    }

    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        property?.SetValue(obj, value);
    }

    private static void SetPrivateField<T>(object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
    }

    private static async Task InvokePrivateMethodAsync(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method!.Invoke(obj, parameters);
        if (result is Task task) await task.ConfigureAwait(false);
    }

    private static async Task<T> InvokePrivateMethodAsync<T>(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method!.Invoke(obj, parameters);
        if (result is Task<T> taskWithResult) return await taskWithResult.ConfigureAwait(false);
        if (result is Task task) 
        {
            await task.ConfigureAwait(false);
            return default!;
        }
        return (T)result!;
    }

    private static T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method!.Invoke(obj, parameters);
        return (T)result!;
    }

    [Test]
    public async Task GetBestImageUrl_Should_Be_Synchronous_And_Fast()
    {
        // Arrange
        var testArticles = CreateTestArticles(100); // Test with many articles

        // Act
        var stopwatch = Stopwatch.StartNew();
        foreach (var article in testArticles)
        {
            var imageUrl = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetBestImageUrl", article).ConfigureAwait(false);
            await Assert.That(imageUrl).IsNotNull();
        }

        stopwatch.Stop();

        // Assert
        // Should be very fast as it's now synchronous and cache-only
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(100);
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Complete_Within_500ms()
    {
        // Arrange
        var testArticles = CreateTestArticles(20); // Test with many articles
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock cache to return null (worst case - no cached results)
        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(500);

        // Verify cache was populated
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");
        await Assert.That(imageUrlCache.Count).IsEqualTo(testArticles.Count);
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Handle_Cached_CORS_Blocked_Images_Quickly()
    {
        // Arrange
        var testArticles = CreateTestArticles(10);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock some images as CORS-blocked in cache
        var blockedImageResult = new ImageValidationResult
        {
            IsValid = false,
            ErrorMessage = "Browser blocked image due to CORS policy"
        };

        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                return imageUrl.Contains("image2") || imageUrl.Contains("image5")
                    ? blockedImageResult
                    : null;
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(500);

        // Verify CORS-blocked images use placeholders
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");
        var blockedImageUrls = imageUrlCache.Values
            .Where(url => url.StartsWith("data:image/svg+xml;base64", StringComparison.OrdinalIgnoreCase))
            .ToList();

        await Assert.That(blockedImageUrls.Count).IsEqualTo(2); // image2 and image5
    }

    [Test]
    public async Task PopulateImageUrlCacheAsync_Should_Handle_Mixed_Cache_Results_Quickly()
    {
        // Arrange
        var testArticles = CreateTestArticles(15);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock mixed cache results
        var validResult = new ImageValidationResult { IsValid = true };
        var blockedResult = new ImageValidationResult
        {
            IsValid = false,
            ErrorMessage = "Browser blocked image due to CORS policy"
        };

        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                if (imageUrl.Contains("image1") || imageUrl.Contains("image3"))
                    return validResult;
                if (imageUrl.Contains("image7") || imageUrl.Contains("image9"))
                    return blockedResult;
                return null; // Not cached
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(500);

        // Verify cache was populated with appropriate URLs
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");
        await Assert.That(imageUrlCache.Count).IsEqualTo(testArticles.Count);

        // Verify placeholders are used for blocked images
        var placeholderCount = imageUrlCache.Values
            .Count(url => url.StartsWith("data:image/svg+xml;base64", StringComparison.OrdinalIgnoreCase));
        await Assert.That(placeholderCount).IsEqualTo(2);
    }

    [Test]
    public async Task ValidateImagesInBackgroundAsync_Should_Not_Block_Initial_Render()
    {
        // Arrange
        var testArticles = CreateTestArticles(10);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);
        SetPrivateField(_articlesComponent, "_isValidatingImages", false);

        // Mock cache to return null (requires validation)
        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        // Mock slow validation to simulate network delay
        _mockImageValidationService.ValidateImageWithCacheAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(100).ConfigureAwait(false); // Simulate network delay
                return new ImageValidationResult { IsValid = true };
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokePrivateMethodAsync(_articlesComponent, "ValidateImagesInBackgroundAsync").ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        // Background validation should complete quickly even with network delays
        // because it uses parallel processing with concurrency control
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(1000);

        // Verify validation flag is reset
        var isValidatingImages = GetPrivateField<bool>(_articlesComponent, "_isValidatingImages");
        await Assert.That(isValidatingImages).IsFalse();
    }
}

// Mock classes for testing
public class MockNavigationManager : NavigationManager
{
    public MockNavigationManager()
    {
        Initialize("https://localhost/", "https://localhost/");
    }
}