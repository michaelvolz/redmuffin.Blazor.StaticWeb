using Bunit;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Tests.Support.Images;
using redmuffin.Blazor.StaticWeb.Tests.Support.Logging;
using redmuffin.Blazor.StaticWeb.Tests.Support.Mediator;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Pages.Articles.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

[Category("Feature:Articles")]
public partial class ArticlesTests
{
    // Helper method to create test RaindropItem
    public static RaindropItem CreateTestItem(
        int id = 1,
        string title = "Test Article",
        string excerpt = "Test excerpt",
        string link = "https://example.com/test",
        string cover = "https://example.com/cover.jpg"
    )
    {
        return new RaindropItem
        {
            Id = id,
            Title = title,
            Excerpt = excerpt,
            Link = link,
            Cover = cover,
            Type = "article",
        };
    }

    // Factory methods for creating test scopes - optimized for fast execution
    private static TestScope CreateTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithStandardServices();
    }

    private static TestScope CreateFailingAPITestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithFailingMediator();
    }

    private static TestScope CreateEmptyArticlesTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithEmptyArticles();
    }

    /// <summary>
    ///     Modern test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
    {
        public BunitContext BUnitContext { get; } = new();
        public NavigationManager_Mock NavigationManager { get; } = new(baseUri);
        public Logger_Spy<ArticlesComponent> Logger { get; } = new();
        public ImagePlaceholderService_Mock ImagePlaceholderService { get; } = new();
        public ImageUrlResolver_Mock ImageUrlResolver { get; } = new();
        public RaindropMediator_Mock Mediator_Mock { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            Mediator_Mock.SetupLoad(DefaultArticles());
            Mediator_Mock.SetupRefresh(DefaultArticles());
            ImagePlaceholderService.SetupDefaultPlaceholder("/images/placeholder.svg");
            RegisterCoreServices();
            return this;
        }

        /// <summary>
        ///     Configures the test context with a failing Raindrop Mediator for error testing scenarios.
        /// </summary>
        public TestScope WithFailingMediator()
        {
            Mediator_Mock.SetupLoadFailure();
            Mediator_Mock.SetupRefreshFailure();
            RegisterCoreServices();
            return this;
        }

        /// <summary>
        ///     Configures the test context with empty articles for testing empty state scenarios.
        /// </summary>
        public TestScope WithEmptyArticles()
        {
            Mediator_Mock.SetupLoad([]);
            Mediator_Mock.SetupRefresh([]);
            RegisterCoreServices();
            return this;
        }

        private void RegisterCoreServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<ArticlesComponent>>(Logger);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService);
            BUnitContext.Services.AddSingleton<IImageUrlResolver>(ImageUrlResolver);
            BUnitContext.Services.AddSingleton<IMediator>(Mediator_Mock);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static IReadOnlyList<RaindropItem> DefaultArticles() =>
            [
                new()
                {
                    Id = 1,
                    Title = "Test Article 1",
                    Excerpt = "This is a test article excerpt",
                    Link = "https://example.com/article1",
                    Cover = "https://example.com/cover1.jpg",
                    Type = "article",
                },
                new()
                {
                    Id = 2,
                    Title = "Test Article 2",
                    Excerpt =
                        "This is another test article with a longer excerpt that should be truncated when it exceeds the maximum length limit of 250 characters. This text is intentionally long to test the truncation functionality in the Articles component.",
                    Link = "https://example.com/article2",
                    Cover = "https://example.com/cover2.jpg",
                    Type = "article",
                },
            ];

        /// <summary>
        ///     Configures JS interop mode for testing JavaScript integration scenarios.
        /// </summary>
        public TestScope WithJSInterop(JSRuntimeMode mode = JSRuntimeMode.Strict)
        {
            BUnitContext.JSInterop.Mode = mode;
            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    // Mock NavigationManager for testing
    public class NavigationManager_Mock : NavigationManager
    {
        public NavigationManager_Mock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        public string? NavigatedTo { get; private set; }
        public bool NavigationCalled { get; private set; }
        public NavigationOptions? LastNavigationOptions { get; private set; }

        public void Reset()
        {
            NavigatedTo = null;
            NavigationCalled = false;
            LastNavigationOptions = null;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
            NavigationCalled = true;
            LastNavigationOptions = options;
        }
    }
}
