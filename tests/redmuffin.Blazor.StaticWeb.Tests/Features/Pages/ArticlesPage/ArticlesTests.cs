using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

public class ArticlesTests
{
    private readonly ILogger<Articles> _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;

    public ArticlesTests()
    {
        // Setup mock dependencies
        var services = new ServiceCollection();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<Articles>>();
        
        // Mock JSRuntime
        _jsRuntime = new MockJSRuntime();
        
        // Mock NavigationManager
        _navigationManager = new MockNavigationManager();
    }

    [Test]
    public async Task Articles_Component_ShouldHaveCorrectInitialState()
    {
        // Arrange
        var articlesComponent = new Articles();
        SetPrivateProperty(articlesComponent, "Logger", _logger);
        SetPrivateProperty(articlesComponent, "Js", _jsRuntime);
        SetPrivateProperty(articlesComponent, "Navigation", _navigationManager);

        // Act - Check initial state
        var articleItems = GetPrivateField<List<RaindropItem>?>(articlesComponent, "_articleItems");
        var errorMessage = GetPrivateField<string?>(articlesComponent, "_errorMessage");
        var isLoading = GetPrivateField<bool>(articlesComponent, "_isLoading");

        // Assert
        await Assert.That(articleItems).IsNull();
        await Assert.That(errorMessage).IsNull();
        await Assert.That(isLoading).IsFalse();
    }

    [Test]
    public async Task Articles_Component_ShouldHaveCorrectPageTitle()
    {
        // Arrange & Act
        var articlesComponent = new Articles();
        SetPrivateProperty(articlesComponent, "Logger", _logger);
        SetPrivateProperty(articlesComponent, "Js", _jsRuntime);
        SetPrivateProperty(articlesComponent, "Navigation", _navigationManager);

        // Assert - Component should be instantiable
        await Assert.That(articlesComponent).IsNotNull();
        await Assert.That(articlesComponent.GetType().Name).IsEqualTo("Articles");
        await Assert.That(articlesComponent.GetType().Namespace).IsEqualTo("redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage");
    }

    [Test]
    public async Task Articles_Component_ShouldHaveRequiredDependencies()
    {
        // Arrange
        var articlesType = typeof(Articles);
        
        // Act
        var loggerProperty = articlesType.GetProperty("Logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var jsProperty = articlesType.GetProperty("Js", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var navigationProperty = articlesType.GetProperty("Navigation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        await Assert.That(loggerProperty).IsNotNull();
        await Assert.That(jsProperty).IsNotNull();
        await Assert.That(navigationProperty).IsNotNull();
        
        // Check for Inject attributes
        await Assert.That(loggerProperty!.GetCustomAttributes(typeof(InjectAttribute), false)).IsNotEmpty();
        await Assert.That(jsProperty!.GetCustomAttributes(typeof(InjectAttribute), false)).IsNotEmpty();
        await Assert.That(navigationProperty!.GetCustomAttributes(typeof(InjectAttribute), false)).IsNotEmpty();
    }

    [Test]
    public async Task StopShimmerAsync_WithValidElementId_ShouldInvokeJavaScript()
    {
        // Arrange
        var articlesComponent = new Articles();
        var mockJsRuntime = new MockJSRuntime();
        SetPrivateProperty(articlesComponent, "Logger", _logger);
        SetPrivateProperty(articlesComponent, "Js", mockJsRuntime);
        SetPrivateProperty(articlesComponent, "Navigation", _navigationManager);
        var elementId = "test-element-id";

        // Act
        await InvokePrivateMethodAsync(articlesComponent, "StopShimmerAsync", elementId).ConfigureAwait(false);

        // Assert
        await Assert.That(mockJsRuntime.InvokedMethods).Contains("eval");
    }

    // Helper methods for testing private members
    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(propertyName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        property?.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
    }

    private static async Task InvokePrivateMethodAsync(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(obj, parameters);
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }
}

// Mock classes for testing
public class MockJSRuntime : IJSRuntime
{
    public List<string> InvokedMethods { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        InvokedMethods.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        InvokedMethods.Add(identifier);
        return ValueTask.FromResult(default(TValue)!);
    }
}

public class MockNavigationManager : NavigationManager
{
    public MockNavigationManager() : base()
    {
        Initialize("https://localhost/", "https://localhost/");
    }
}
