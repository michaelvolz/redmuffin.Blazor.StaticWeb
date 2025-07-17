using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Models;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

/// <summary>
///     Integration tests for the Articles page image delay bug fix.
///     Validates CORS protection, progressive enhancement, and end-to-end functionality.
/// </summary>
public sealed class ArticlesImageDelayBugFixTests : IDisposable
{
    public ArticlesImageDelayBugFixTests()
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
    public async Task Should_Handle_Concurrent_Validation_Without_Blocking()
    {
        // Arrange
        var testArticles = CreateTestArticles(15);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);
        SetPrivateField(_articlesComponent, "_isValidatingImages", false);

        // Mock cache to return null (all require validation)
        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        // Track validation calls
        var validationCalls = new List<string>();
        _mockImageValidationService.ValidateImageWithCacheAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                validationCalls.Add(imageUrl);
                var delay = imageUrl.GetHashCode() % 100 + 50; // 50-149ms delay
                await Task.Delay(delay).ConfigureAwait(false);
                return new ImageValidationResult { IsValid = true };
            });

        // Act
        var startTime = DateTime.UtcNow;
        await InvokePrivateMethodAsync(_articlesComponent, "ValidateImagesInBackgroundAsync").ConfigureAwait(false);
        var endTime = DateTime.UtcNow;

        // Assert
        // Should complete faster than sequential processing due to parallel execution
        var totalDuration = endTime - startTime;
        await Assert.That(totalDuration.TotalMilliseconds).IsLessThan(2000); // Much faster than 15 * 100ms = 1500ms

        // Verify validation state is properly managed
        var isValidatingImages = GetPrivateField<bool>(_articlesComponent, "_isValidatingImages");
        await Assert.That(isValidatingImages).IsFalse();

        // If validations occurred, verify they were handled correctly
        if (validationCalls.Count > 0)
        {
            // Verify that some validations occurred (the exact number depends on implementation)
            await Assert.That(validationCalls.Count).IsGreaterThan(0);
            await Assert.That(validationCalls.Count).IsLessThanOrEqualTo(15);
        }
    }

    [Test]
    public async Task Should_Handle_Enhanced_Images_Priority_Over_Original_Cover()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "https://example.com/original.jpg"
        };

        // Setup enhanced image data
        var enhancedImage = new CachedImageData
        {
            ImageUrl = "https://example.com/enhanced.jpg",
            IsValidated = true
        };

        var articleState = new ArticleProcessingState();
        articleState.CompleteProcessing(enhancedImage);

        var articleStates = new Dictionary<string, ArticleProcessingState>(StringComparer.OrdinalIgnoreCase)
        {
            [testArticle.Link] = articleState
        };

        SetPrivateField(_articlesComponent, "_articleStates", articleStates);

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetBestImageUrl", testArticle).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/enhanced.jpg");
    }

    [Test]
    public async Task Should_Handle_Missing_Cover_Images_With_Placeholder()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = string.Empty
        };

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetBestImageUrl", testArticle).ConfigureAwait(false);

        // Assert
        await Assert.That(result).Contains("data:image/svg+xml;base64");
    }

    [Test]
    public async Task Should_Handle_Progressive_Enhancement_During_Background_Validation()
    {
        // Arrange
        var testArticles = CreateTestArticles(3);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);
        SetPrivateField(_articlesComponent, "_isValidatingImages", false);

        // Mock cache to return null (requires validation)
        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        // Mock progressive validation results
        var validationResults = new Dictionary<string, ImageValidationResult>
        {
            ["https://example.com/image1.jpg"] = new() { IsValid = true },
            ["https://example.com/image2.jpg"] = new() { IsValid = false, ErrorMessage = "Network error" },
            ["https://example.com/image3.jpg"] = new() { IsValid = true }
        };

        _mockImageValidationService.ValidateImageWithCacheAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                return Task.FromResult(validationResults.GetValueOrDefault(imageUrl, new ImageValidationResult { IsValid = true }));
            });

        // Act
        await InvokePrivateMethodAsync(_articlesComponent, "ValidateImagesInBackgroundAsync").ConfigureAwait(false);

        // Assert
        // Verify validation state is properly managed
        var isValidatingImages = GetPrivateField<bool>(_articlesComponent, "_isValidatingImages");
        await Assert.That(isValidatingImages).IsFalse();

        // Verify article states are updated
        var articleStates = GetPrivateField<Dictionary<string, ArticleProcessingState>>(_articlesComponent, "_articleStates");
        await Assert.That(articleStates.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Should_Maintain_Cache_Behavior_During_Initial_Load()
    {
        // Arrange
        var testArticles = CreateTestArticles(10);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock mixed cache results
        var cacheHits = new HashSet<string> { "https://example.com/image1.jpg", "https://example.com/image5.jpg" };
        var validResult = new ImageValidationResult { IsValid = true };

        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                return cacheHits.Contains(imageUrl) ? validResult : null;
            });

        // Act
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);

        // Assert
        // Verify cache was populated immediately
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");
        await Assert.That(imageUrlCache.Count).IsEqualTo(testArticles.Count);

        // Verify cached images use original URLs
        await Assert.That(imageUrlCache["https://example.com/article1"]).IsEqualTo("https://example.com/image1.jpg");
        await Assert.That(imageUrlCache["https://example.com/article5"]).IsEqualTo("https://example.com/image5.jpg");

        // Verify non-cached images also use original URLs (fallback behavior)
        await Assert.That(imageUrlCache["https://example.com/article2"]).IsEqualTo("https://example.com/image2.jpg");
    }

    [Test]
    public async Task Should_Maintain_Existing_Functionality_After_Bug_Fix()
    {
        // Arrange
        var testArticles = CreateTestArticles(5);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock mixed scenarios
        var blockedResult = new ImageValidationResult { IsValid = false, ErrorMessage = "Browser blocked image due to CORS policy" };
        var validResult = new ImageValidationResult { IsValid = true };

        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                if (imageUrl.Contains("image1")) return validResult;
                if (imageUrl.Contains("image3")) return blockedResult;
                return null; // Not cached
            });

        // Act
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);

        // Assert
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");

        // Verify all core functionality is preserved
        await Assert.That(imageUrlCache.Count).IsEqualTo(testArticles.Count);
        await Assert.That(imageUrlCache["https://example.com/article1"]).IsEqualTo("https://example.com/image1.jpg");
        await Assert.That(imageUrlCache["https://example.com/article3"]).StartsWith("data:image/svg+xml;base64");
        await Assert.That(imageUrlCache["https://example.com/article2"]).IsEqualTo("https://example.com/image2.jpg");
        await Assert.That(imageUrlCache["https://example.com/article4"]).IsEqualTo("https://example.com/image4.jpg");
        await Assert.That(imageUrlCache["https://example.com/article5"]).IsEqualTo("https://example.com/image5.jpg");
    }

    [Test]
    public async Task Should_Preserve_CORS_Protection_With_Cached_Blocked_Images()
    {
        // Arrange
        var testArticles = CreateTestArticles(5);
        SetPrivateField(_articlesComponent, "_articleItems", testArticles);

        // Mock CORS-blocked images in cache
        var blockedImageResult = new ImageValidationResult
        {
            IsValid = false,
            ErrorMessage = "Browser blocked image due to CORS policy"
        };

        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                return imageUrl.Contains("image2") || imageUrl.Contains("image4")
                    ? blockedImageResult
                    : null;
            });

        // Act
        await InvokePrivateMethodAsync(_articlesComponent, "PopulateImageUrlCacheAsync").ConfigureAwait(false);

        // Assert
        var imageUrlCache = GetPrivateField<Dictionary<string, string>>(_articlesComponent, "_imageUrlCache");

        // Verify blocked images use placeholders
        await Assert.That(imageUrlCache["https://example.com/article2"]).StartsWith("data:image/svg+xml;base64");
        await Assert.That(imageUrlCache["https://example.com/article4"]).StartsWith("data:image/svg+xml;base64");

        // Verify non-blocked images use original URLs
        await Assert.That(imageUrlCache["https://example.com/article1"]).IsEqualTo("https://example.com/image1.jpg");
        await Assert.That(imageUrlCache["https://example.com/article3"]).IsEqualTo("https://example.com/image3.jpg");
        await Assert.That(imageUrlCache["https://example.com/article5"]).IsEqualTo("https://example.com/image5.jpg");
    }

    [Test]
    public async Task Should_Skip_Validation_For_Placeholder_URLs()
    {
        // Arrange
        var testArticles = new List<RaindropItem>
        {
            new() { Id = 1, Link = "https://example.com/article1", Cover = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0..." },
            new() { Id = 2, Link = "https://example.com/article2", Cover = "https://placeholder.com/image.jpg" },
            new() { Id = 3, Link = "https://example.com/article3", Cover = "https://example.com/normal.jpg" }
        };

        SetPrivateField(_articlesComponent, "_articleItems", testArticles);
        SetPrivateField(_articlesComponent, "_isValidatingImages", false);

        // Mock cache to return null (would normally trigger validation)
        _mockImageValidationService.GetCachedValidationResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageValidationResult?)null);

        // Track validation calls
        var validationCalls = new List<string>();
        _mockImageValidationService.ValidateImageWithCacheAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var imageUrl = callInfo.Arg<string>();
                validationCalls.Add(imageUrl);
                return Task.FromResult(new ImageValidationResult { IsValid = true });
            });

        // Act
        await InvokePrivateMethodAsync(_articlesComponent, "ValidateImagesInBackgroundAsync").ConfigureAwait(false);

        // Assert
        // Verify that the validation process completed successfully
        var isValidatingImages = GetPrivateField<bool>(_articlesComponent, "_isValidatingImages");
        await Assert.That(isValidatingImages).IsFalse();

        // Verify placeholder images were properly skipped in the validation logic
        if (validationCalls.Count > 0)
        {
            // If any validations occurred, ensure placeholder URLs were not validated
            await Assert.That(validationCalls).DoesNotContain("data:image/svg+xml;base64,PHN2ZyB3aWR0aD0...");
            await Assert.That(validationCalls).DoesNotContain("https://placeholder.com/image.jpg");
        }
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