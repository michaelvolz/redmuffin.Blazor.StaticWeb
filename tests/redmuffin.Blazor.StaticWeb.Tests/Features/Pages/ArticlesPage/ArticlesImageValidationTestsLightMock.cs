// This file has been migrated from NSubstitute to LightMock.Generator.
// All mocks have been updated to use The<> syntax and Arrange/Assert patterns.

using System.Reflection;
using LightMock.Generator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

/// <summary>
///     Tests for the Articles component image validation and delay bug fix.
///     Focuses on the background validation process and cache-first rendering approach.
/// </summary>
public sealed class ArticlesImageValidationTestsLightMock : IDisposable
{
    public ArticlesImageValidationTestsLightMock()
    {
        // Setup mock dependencies
        _mockImageValidationService = new Mock<ISimpleImageValidationService>();
        _loggerMock = new Mock<ILogger<Articles>>();
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _httpClientMock = new Mock<HttpClient>();

        // Create component instance
        _articlesComponent = new Articles();

        // Set up dependencies using reflection
        SetPrivateProperty(_articlesComponent, "SimpleImageValidationService", _mockImageValidationService.Object);
        SetPrivateProperty(_articlesComponent, "Logger", _loggerMock.Object);
        SetPrivateProperty(_articlesComponent, "Js", _jsRuntimeMock.Object);
        SetPrivateProperty(_articlesComponent, "Navigation", new LightMockNavigationManagerForValidation());
        SetPrivateProperty(_articlesComponent, "Http", _httpClientMock.Object);

        // Get the validation semaphore for testing concurrency (if it exists)
        try
        {
            _validationSemaphore = GetPrivateField<SemaphoreSlim>(_articlesComponent, "_validationSemaphore");
        }
        catch
        {
            _validationSemaphore = null; // Field doesn't exist in current implementation
        }
    }

    public void Dispose()
    {
        _validationSemaphore?.Dispose();
    }

    private readonly Articles _articlesComponent;
    private readonly Mock<ISimpleImageValidationService> _mockImageValidationService;
    private readonly Mock<ILogger<Articles>> _loggerMock;
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly SemaphoreSlim? _validationSemaphore;

    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        property?.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
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

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Placeholder_For_Cached_Invalid_Result()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "https://example.com/blocked.jpg"
        };

        var invalidResult = ImageValidationResult.Failure("CORS blocked");
        _mockImageValidationService.Arrange(s => s.GetCachedResultAsync("https://example.com/blocked.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(invalidResult));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetCachedImageUrlAsync", testArticle).ConfigureAwait(false);

        // Assert - Should return placeholder for cached invalid result
        await Assert.That(result).Contains("data:image/svg+xml;base64");
        // Don't check for specific base64 content since SVG generation may vary
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Return_Placeholder_When_No_Cover_Available()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = string.Empty
        };

        // Mock GetCachedResultAsync to return null for empty cover
        _mockImageValidationService.Arrange(s => s.GetCachedResultAsync(string.Empty, CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetCachedImageUrlAsync", testArticle).ConfigureAwait(false);

        // Assert
        await Assert.That(result).Contains("data:image/svg+xml;base64");
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Use_Cached_Valid_Result()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "https://example.com/original.jpg"
        };

        var validResult = ImageValidationResult.Success();
        _mockImageValidationService.Arrange(s => s.GetCachedResultAsync("https://example.com/original.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(validResult));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetCachedImageUrlAsync", testArticle).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/original.jpg");
    }

    [Test]
    public async Task GetCachedImageUrlAsync_Should_Use_Original_Cover_When_No_Cache()
    {
        // Arrange
        var testArticle = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "https://example.com/original.jpg"
        };

        // Mock GetCachedResultAsync to return null (no cache)
        _mockImageValidationService.Arrange(s => s.GetCachedResultAsync("https://example.com/original.jpg", CancellationToken.None))
            .Returns(Task.FromResult<ImageValidationResult?>(null));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetCachedImageUrlAsync", testArticle).ConfigureAwait(false);

        // Assert - Should return original cover image when no cache entry exists
        await Assert.That(result).IsEqualTo("https://example.com/original.jpg");
    }
}

// Mock classes for testing
public class LightMockNavigationManagerForValidation : NavigationManager
{
    public LightMockNavigationManagerForValidation()
    {
        Initialize("https://localhost/", "https://localhost/");
    }
}