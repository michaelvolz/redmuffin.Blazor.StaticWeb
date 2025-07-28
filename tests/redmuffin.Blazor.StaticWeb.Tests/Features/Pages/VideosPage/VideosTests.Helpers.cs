using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.VideosPage;

public sealed partial class VideosTests
{
    /// <summary>
    ///     Creates a test scope with all necessary dependencies for Videos component testing.
    /// </summary>
    /// <returns>A configured TestScope instance</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    /// <summary>
    ///     Creates a test video item for testing purposes.
    /// </summary>
    /// <param name="id">The video ID</param>
    /// <param name="title">The video title</param>
    /// <param name="excerpt">The video excerpt</param>
    /// <param name="link">The video link</param>
    /// <returns>A configured RaindropItem for testing</returns>
    private static RaindropItem CreateTestVideo(string id, string title, string excerpt, string link)
    {
        return new RaindropItem
        {
            Id = int.Parse(id),
            Title = title,
            Excerpt = excerpt,
            Link = link,
            Cover = $"https://example.com/cover{id}.jpg",
            Created = DateTime.UtcNow,
            Type = "video",
            Domain = "example.com"
        };
    }

    /// <summary>
    ///     Modern test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestScope(string baseUri = "http://localhost:5000/")
        {
            BUnitContext = new BunitContext();
            NavigationManager = new NavigationManagerMock(baseUri);
            Logger = new TestLogger<Videos>();
            RaindropAPIMock = new RaindropAPIMock();
            ImagePlaceholderServiceMock = new ImagePlaceholderServiceMock();
            SimpleImageValidationServiceMock = new SimpleImageValidationServiceMock();

            // Create actual service instance with mocked dependencies
            ImageValidationCacheService = new ImageValidationCacheService(
                SimpleImageValidationServiceMock,
                ImagePlaceholderServiceMock,
                new TestLogger<ImageValidationCacheService>());
        }

        public BunitContext BUnitContext { get; }
        public NavigationManagerMock NavigationManager { get; }
        public TestLogger<Videos> Logger { get; }
        public RaindropAPIMock RaindropAPIMock { get; }
        public ImagePlaceholderServiceMock ImagePlaceholderServiceMock { get; }
        public SimpleImageValidationServiceMock SimpleImageValidationServiceMock { get; }
        public IImageValidationCacheService ImageValidationCacheService { get; }

        /// <summary>
        ///     Sets up default behaviors for mocks to ensure tests run smoothly.
        /// </summary>
        private static void SetupDefaultMockBehaviors()
        {
            // Default RaindropAPI behavior - no setup needed for manual mock

            // Default ImagePlaceholderService behaviors - no setup needed for manual mock
        }

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<Videos>>(Logger);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPIMock);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderServiceMock);
            BUnitContext.Services.AddSingleton(ImageValidationCacheService);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;

            // Set up default mock behaviors
            SetupDefaultMockBehaviors();

            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    /// <summary>
    ///     Test logger implementation for capturing log messages during tests.
    /// </summary>
    public sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
    }

    /// <summary>
    ///     Represents a log entry for testing purposes.
    /// </summary>
    public sealed class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    ///     Mock NavigationManager for testing.
    /// </summary>
    public sealed class NavigationManagerMock : NavigationManager
    {
        public NavigationManagerMock(string baseUri = "http://localhost:5000/")
        {
            Initialize(baseUri, baseUri);
        }

        public string? NavigatedTo { get; private set; }
        public bool NavigationCalled { get; private set; }

        public void Reset()
        {
            NavigatedTo = null;
            NavigationCalled = false;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
            NavigationCalled = true;
        }
    }

    /// <summary>
    ///     Manual mock implementation for ISimpleImageValidationService since LightMock.Generator doesn't support it.
    /// </summary>
    public sealed class SimpleImageValidationServiceMock : ISimpleImageValidationService
    {
        private readonly Dictionary<string, ImageValidationResult?> _cachedResults = new();
        private readonly Dictionary<string, ImageValidationResult> _validationResults = new();
        private readonly Dictionary<string, string> _placeholderResults = new();

        /// <summary>
        ///     Sets up a cached result for testing.
        /// </summary>
        public void SetupCachedResult(string imageUrl, ImageValidationResult? result)
        {
            _cachedResults[imageUrl] = result;
        }

        /// <summary>
        ///     Sets up a validation result for testing.
        /// </summary>
        public void SetupValidationResult(string imageUrl, ImageValidationResult result)
        {
            _validationResults[imageUrl] = result;
        }

        /// <summary>
        ///     Sets up a placeholder result for testing.
        /// </summary>
        public void SetupPlaceholderResult(string imageUrl, string placeholder)
        {
            _placeholderResults[imageUrl] = placeholder;
        }

        /// <summary>
        ///     Clears all setup results.
        /// </summary>
        public void Reset()
        {
            _cachedResults.Clear();
            _validationResults.Clear();
            _placeholderResults.Clear();
        }

        public Task<ImageValidationResult?> GetCachedResultAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            _cachedResults.TryGetValue(imageUrl, out var result);
            return Task.FromResult(result);
        }

        public Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            if (_validationResults.TryGetValue(imageUrl, out var result)) return Task.FromResult(result);
            return Task.FromResult(ImageValidationResult.Success());
        }

        public Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            if (_placeholderResults.TryGetValue(imageUrl, out var placeholder)) return Task.FromResult(placeholder);
            return Task.FromResult(imageUrl); // Return original URL by default
        }
    }

    /// <summary>
    ///     Manual mock implementation for IImagePlaceholderService since LightMock.Generator doesn't support it.
    /// </summary>
    public sealed class ImagePlaceholderServiceMock : IImagePlaceholderService
    {
        private readonly Dictionary<string, string> _imageUrls = new();
        private readonly Dictionary<string, bool> _fallbackStatuses = new();
        private readonly Dictionary<string, string> _fallbackReasons = new();
        private readonly Dictionary<string, string> _simplePlaceholders = new();
        private string _defaultPlaceholder = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCI+PC9zdmc+";

        public void SetupDefaultPlaceholder(string placeholder)
        {
            _defaultPlaceholder = placeholder;
        }

        public void SetupSimplePlaceholder(string reason, string result)
        {
            _simplePlaceholders[reason] = result;
        }

        public void SetupImageUrl(string itemLink, string resultUrl)
        {
            _imageUrls[itemLink] = resultUrl;
        }

        public void SetupFallbackStatus(string itemLink, bool hasFallback)
        {
            _fallbackStatuses[itemLink] = hasFallback;
        }

        public void SetupFallbackReason(string itemLink, string reason)
        {
            _fallbackReasons[itemLink] = reason;
        }

        public void Reset()
        {
            _imageUrls.Clear();
            _fallbackStatuses.Clear();
            _fallbackReasons.Clear();
            _simplePlaceholders.Clear();
            _defaultPlaceholder = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCI+PC9zdmc+";
        }

        public string GetDefaultPlaceholder()
        {
            return _defaultPlaceholder;
        }

        public string GenerateSimplePlaceholder(string reason)
        {
            return _simplePlaceholders.TryGetValue(reason, out var placeholder) ? placeholder : $"data:image/svg+xml;base64,placeholder-{reason}";
        }

        public string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            var key = item.Link;
            return _imageUrls.TryGetValue(key, out var url) ? url : imageUrlCache.TryGetValue(key, out var cachedUrl) ? cachedUrl : _defaultPlaceholder;
        }

        public Task HandleImageLoadAsync(
            string elementId,
            string itemLink,
            bool loadSuccess,
            IDictionary<string, string> imageUrlCache,
            IJSRuntime jsRuntime,
            Func<Task> stateHasChangedCallback)
        {
            return Task.CompletedTask;
        }

        public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return _fallbackStatuses.TryGetValue(item.Link, out var status) && status;
        }

        public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return _fallbackReasons.TryGetValue(item.Link, out var reason) ? reason : string.Empty;
        }
    }

    /// <summary>
    ///     Manual mock implementation for IRaindropAPI since LightMock.Generator doesn't support it.
    /// </summary>
    public sealed class RaindropAPIMock : IRaindropAPI
    {
        private readonly List<RaindropItem> _videos = new();
        private readonly List<RaindropItem> _articles = new();
        private Exception? _videosException;
        private Exception? _articlesException;

        public void SetupVideos(IEnumerable<RaindropItem> videos)
        {
            _videos.Clear();
            _videos.AddRange(videos);
        }

        public void SetupArticles(IEnumerable<RaindropItem> articles)
        {
            _articles.Clear();
            _articles.AddRange(articles);
        }

        public void SetupVideosException(Exception exception)
        {
            _videosException = exception;
        }

        public void SetupArticlesException(Exception exception)
        {
            _articlesException = exception;
        }

        public void Reset()
        {
            _videos.Clear();
            _articles.Clear();
            _videosException = null;
            _articlesException = null;
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_videosException != null) throw _videosException;

            return Task.FromResult<IEnumerable<RaindropItem>>(_videos);
        }

        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_articlesException != null) throw _articlesException;

            return Task.FromResult<IEnumerable<RaindropItem>>(_articles);
        }
    }
}