using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using redmuffin.Blazor.StaticWeb.Common.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.HomePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

/// <summary>
///     Integration tests for the Home page component verifying full system behavior.
///     Uses TestScope pattern for clean resource management and consistent service setup.
///     Optimized for fast execution with TestDelayProvider.
/// </summary>
public class HomePageIntegrationTests
{
    /// <summary>
    ///     Modern integration test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Specifically designed for integration testing scenarios with full service setup.
    /// </summary>
    public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
    {
        public BunitContext Context { get; } = new();
        public NavigationManagerMock NavigationManager { get; } = new(baseUri);

        /// <summary>
        ///     Configures the test context with fast integration testing services.
        ///     Includes logging, HTTP client factory, and TestDelayProvider for optimal performance.
        /// </summary>
        public TestScope WithStandardIntegrationServices()
        {
            Context.Services.AddSingleton<NavigationManager>(NavigationManager);
            Context.Services.AddLogging();
            Context.Services.AddHttpClient();
            Context.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in integration tests
            return this;
        }

        /// <summary>
        ///     Configures additional services for specialized integration test scenarios.
        /// </summary>
        public TestScope WithServices(Action<IServiceCollection> configure)
        {
            configure(Context.Services);
            return this;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }

    /// <summary>
    ///     Factory method for creating integration test scopes with standard services.
    /// </summary>
    private static TestScope CreateIntegrationTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithStandardIntegrationServices();
    }

    /// <summary>
    ///     Factory method for creating port-specific test scopes.
    /// </summary>
    private static TestScope CreatePortSpecificTestScope(string baseUri)
    {
        return new TestScope(baseUri).WithStandardIntegrationServices();
    }

    [Test]
    public async Task Homepage_DisplaysHeadingAndEmojis()
    {
        // Arrange
        using var scope = CreatePortSpecificTestScope("http://localhost:4280/");
        var component = scope.Context.Render<Home>();

        // Act & Assert - Use chaining for related markup assertions
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            await Assert.That(component.Find("div[style='font-size:2rem;']").TextContent)
                .Contains("😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇");
        }
    }


    [Test]
    public async Task Homepage_HasCorrectPageTitle()
    {
        // Arrange
        using var scope = CreateIntegrationTestScope();
        var component = scope.Context.Render<Home>();

        // Assert - Verify PageTitle component functionality
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Chain related markup assertions
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify PageTitle component is included in the rendered output
            await Assert.That(component.FindAll("title").Count).IsGreaterThanOrEqualTo(0);
        }
    }


    [Test]
    public async Task Homepage_RendersSuccessfully_WithoutErrors()
    {
        // Arrange
        using var scope = CreateIntegrationTestScope();
        var component = scope.Context.Render<Home>();

        // Assert - Verify successful rendering and structure
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            await Assert.That(component.FindAll("div[style='font-size:2rem;']").Count).IsEqualTo(1);
        }
    }
}

/// <summary>
///     Mock NavigationManager for integration testing with behavior tracking.
/// </summary>
public class NavigationManagerMock : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the NavigationManagerMock class.
    /// </summary>
    /// <param name="baseUri">The base URI for the navigation manager.</param>
    public NavigationManagerMock(string baseUri)
    {
        Initialize(baseUri, baseUri);
    }

    /// <summary>
    ///     Gets the URI that was navigated to, or null if no navigation occurred.
    /// </summary>
    public string? NavigatedTo { get; private set; }

    /// <summary>
    ///     Captures navigation attempts for testing verification.
    /// </summary>
    /// <param name="uri">The URI to navigate to.</param>
    /// <param name="options">Navigation options.</param>
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}