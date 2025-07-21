using System.Reflection;
using LightMock;
using LightMock.Generator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.UserAcceptance;

/// <summary>
///     Simple test to verify the core functionality works with LightMock.Generator.
/// </summary>
public sealed class ImageDelayBugFixLightMockTest : IDisposable
{
    public ImageDelayBugFixLightMockTest()
    {
        // Setup mock dependencies using LightMock.Generator
        _mockImageValidationService = new Mock<ISimpleImageValidationService>();
        _mockOpenGraphImagesService = new Mock<IOpenGraphImagesService>();
        _mockLogger = new Mock<ILogger<Articles>>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockHttpClient = new Mock<HttpClient>();

        // Create component instance
        _articlesComponent = new Articles();

        // Set up dependencies using reflection
        SetPrivateProperty(_articlesComponent, "SimpleImageValidationService", _mockImageValidationService.Object);
        SetPrivateProperty(_articlesComponent, "OpenGraphImagesService", _mockOpenGraphImagesService.Object);
        SetPrivateProperty(_articlesComponent, "Logger", _mockLogger.Object);
        SetPrivateProperty(_articlesComponent, "Js", _mockJsRuntime.Object);
        SetPrivateProperty(_articlesComponent, "Navigation", new NavigationManagerMock());
        SetPrivateProperty(_articlesComponent, "Http", _mockHttpClient.Object);
    }

    public void Dispose()
    {
        // Nothing to dispose for this simple test
    }

    private readonly Articles _articlesComponent;
    private readonly Mock<ISimpleImageValidationService> _mockImageValidationService;
    private readonly Mock<IOpenGraphImagesService> _mockOpenGraphImagesService;
    private readonly Mock<ILogger<Articles>> _mockLogger;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Mock<HttpClient> _mockHttpClient;

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
        if (result is Task<T> taskResult) return await taskResult.ConfigureAwait(false);
        return (T)result!;
    }

    [Test]
    public async Task GetImageUrlAsync_Should_Return_Placeholder_For_Empty_Cover()
    {
        // Arrange
        var article = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "",
            Title = "Test Article",
            Excerpt = "Test excerpt",
            Created = DateTime.UtcNow
        };

        // Setup mock to return placeholder for empty URL
        _mockImageValidationService.Arrange(x => x.GetImageUrlOrPlaceholderAsync(
                The<string>.IsAnyValue, The<CancellationToken>.IsAnyValue))
            .Returns(Task.FromResult("data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4="));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetImageUrlAsync", article).ConfigureAwait(false);

        // Assert
        await Assert.That(result).StartsWith("data:image/svg+xml;base64,");
    }

    [Test]
    public async Task GetImageUrlAsync_Should_Use_Service_For_Valid_Cover()
    {
        // Arrange
        var article = new RaindropItem
        {
            Id = 1,
            Link = "https://example.com/article1",
            Cover = "https://example.com/image1.jpg",
            Title = "Test Article",
            Excerpt = "Test excerpt",
            Created = DateTime.UtcNow
        };

        // Setup mock to return the original URL when called with any string
        _mockImageValidationService.Arrange(x => x.GetImageUrlOrPlaceholderAsync(
                The<string>.IsAnyValue, The<CancellationToken>.IsAnyValue))
            .Returns<string, CancellationToken>((url, token) => Task.FromResult(url));

        // Act
        var result = await InvokePrivateMethodAsync<string>(_articlesComponent, "GetImageUrlAsync", article).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("https://example.com/image1.jpg");
    }
}

// Mock classes for testing
public class NavigationManagerMock : NavigationManager
{
    public NavigationManagerMock()
    {
        Initialize("https://localhost/", "https://localhost/");
    }
}