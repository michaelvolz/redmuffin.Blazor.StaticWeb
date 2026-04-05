using Bunit;
using LightMock.Generator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Articles;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

[Category("Feature:Articles")]
public partial class ArticlesTests
{
    // Helper method to create test RaindropItem
    public static RaindropItem CreateTestItem(int id = 1, string title = "Test Article", string excerpt = "Test excerpt",
        string link = "https://example.com/test", string cover = "https://example.com/cover.jpg")
    {
        return new RaindropItem
        {
            Id = id,
            Title = title,
            Excerpt = excerpt,
            Link = link,
            Cover = cover,
            Type = "article"
        };
    }

    // Factory methods for creating test scopes - optimized for fast execution
    private static TestScope CreateTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithStandardServices();
    }

    private static TestScope CreateFailingAPITestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithFailingRaindropAPI();
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
        public ImageValidationCacheService_Mock ImageValidationCacheService { get; } = new();
        public RaindropAPI_Mock RaindropAPI { get; } = new();
        public Mock<IRaindropItemsCache> RaindropItemsCache_Mock { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<ArticlesComponent>>(Logger);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService);
            BUnitContext.Services.AddSingleton<IImageValidationCacheService>(ImageValidationCacheService);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPI);
            BUnitContext.Services.AddSingleton<IRaindropItemsCache>(RaindropItemsCache_Mock.Object);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a failing Raindrop API for error testing scenarios.
        /// </summary>
        public TestScope WithFailingRaindropAPI()
        {
            var failingAPI = new RaindropAPI_FailingMock();
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<ArticlesComponent>>(Logger);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService);
            BUnitContext.Services.AddSingleton<IImageValidationCacheService>(ImageValidationCacheService);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(failingAPI);
            BUnitContext.Services.AddSingleton<IRaindropItemsCache>(RaindropItemsCache_Mock.Object);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with empty articles for testing empty state scenarios.
        /// </summary>
        public TestScope WithEmptyArticles()
        {
            var emptyAPI = new RaindropAPI_EmptyMock();
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<ArticlesComponent>>(Logger);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService);
            BUnitContext.Services.AddSingleton<IImageValidationCacheService>(ImageValidationCacheService);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(emptyAPI);
            BUnitContext.Services.AddSingleton<IRaindropItemsCache>(RaindropItemsCache_Mock.Object);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

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

    // Test logger to capture log messages
    public class Logger_Spy<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoOpDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }

        public class LogEntry
        {
            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public string Message { get; set; } = string.Empty;
            public Exception? Exception { get; set; }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    // Mock ImagePlaceholderService for testing
    public class ImagePlaceholderService_Mock : IImagePlaceholderService
    {
        public string GetDefaultPlaceholder()
        {
            return "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCI+PC9zdmc+";
        }

        public string GenerateSimplePlaceholder(string reason)
        {
            return "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCI+PHRleHQ+e3JlYXNvbn08L3RleHQ+PC9zdmc+";
        }

        public string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            var key = item.Link ?? item.Id.ToString();
            if (imageUrlCache.TryGetValue(key, out var cachedUrl)) return cachedUrl;
            return "/images/placeholder.svg";
        }

        public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return "Test fallback reason";
        }

        public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            var key = item.Link ?? item.Id.ToString();
            return !imageUrlCache.ContainsKey(key);
        }

        public Task HandleImageLoadAsync(string elementId, string itemLink, bool loadSuccess, IDictionary<string, string> imageUrlCache, IJSRuntime jsRuntime,
            Func<Task> stateHasChangedCallback)
        {
            if (loadSuccess) imageUrlCache[itemLink] = "https://example.com/loaded-image.jpg";
            return stateHasChangedCallback();
        }
    }

    // Mock ImageValidationCacheService for testing
    public class ImageValidationCacheService_Mock : IImageValidationCacheService
    {
        public Task PopulateImageUrlCacheAsync(IEnumerable<RaindropItem> items, IDictionary<string, string> imageUrlCache, Func<Task> stateHasChangedCallback,
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item.Cover))
                {
                    var key = item.Link ?? item.Id.ToString();
                    imageUrlCache[key] = item.Cover;
                }

            return stateHasChangedCallback();
        }

        public Task<string> GetCachedImageUrlAsync(RaindropItem item, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(item.Cover ?? "/images/placeholder.svg");
        }

        public Task ValidateImageInBackgroundAsync(RaindropItem item, IDictionary<string, string> imageUrlCache, Func<Task> stateHasChangedCallback,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // Mock RaindropAPI for testing
    public class RaindropAPI_Mock : IRaindropAPI
    {
        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            var articles = new List<RaindropItem>
            {
                new()
                {
                    Id = 1,
                    Title = "Test Article 1",
                    Excerpt = "This is a test article excerpt",
                    Link = "https://example.com/article1",
                    Cover = "https://example.com/cover1.jpg",
                    Type = "article"
                },
                new()
                {
                    Id = 2,
                    Title = "Test Article 2",
                    Excerpt =
                        "This is another test article with a longer excerpt that should be truncated when it exceeds the maximum length limit of 250 characters. This text is intentionally long to test the truncation functionality in the Articles component.",
                    Link = "https://example.com/article2",
                    Cover = "https://example.com/cover2.jpg",
                    Type = "article"
                }
            };
            return Task.FromResult<IEnumerable<RaindropItem>>(articles);
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<RaindropItem>>(new List<RaindropItem>());
        }
    }

    // Failing RaindropAPI mock for error testing
    public class RaindropAPI_FailingMock : IRaindropAPI
    {
        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException("Simulated API failure");
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException("Simulated API failure");
        }
    }

    // Empty RaindropAPI mock for empty state testing
    public class RaindropAPI_EmptyMock : IRaindropAPI
    {
        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<RaindropItem>>(new List<RaindropItem>());
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<RaindropItem>>(new List<RaindropItem>());
        }
    }
}