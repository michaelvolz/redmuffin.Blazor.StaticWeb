using Bunit;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Videos;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Videos.Tests;

[Category("Feature:Videos")]
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
            NavigationManager = new NavigationManager_Mock(baseUri);
            Logger = new Logger_Spy<Videos>();
            Mediator_Mock = new RaindropMediator_Mock();
            ImagePlaceholderService_Mock = new ImagePlaceholderService_Mock();
            ImageUrlResolver = new ImageUrlResolver_Mock();
        }

        public BunitContext BUnitContext { get; }
        public NavigationManager_Mock NavigationManager { get; }
        public Logger_Spy<Videos> Logger { get; }
        public RaindropMediator_Mock Mediator_Mock { get; }
        public ImagePlaceholderService_Mock ImagePlaceholderService_Mock { get; }
        public ImageUrlResolver_Mock ImageUrlResolver { get; }

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<Videos>>(Logger);
            BUnitContext.Services.AddSingleton<IMediator>(Mediator_Mock);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService_Mock);
            BUnitContext.Services.AddSingleton<IImageUrlResolver>(ImageUrlResolver);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;

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
    public sealed class Logger_Spy<T> : ILogger<T>
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
    public sealed class NavigationManager_Mock : NavigationManager
    {
        public NavigationManager_Mock(string baseUri = "http://localhost:5000/")
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
    ///     Manual mock for <see cref="IImageUrlResolver"/> so module tests stay free of host Core.
    /// </summary>
    public sealed class ImageUrlResolver_Mock : IImageUrlResolver
    {
        public Task PopulateImageUrlCacheAsync(
            IEnumerable<RaindropItem> items,
            IDictionary<string, string> imageUrlCache,
            Func<Task> stateHasChangedCallback,
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

        public Task ValidateImageInBackgroundAsync(
            RaindropItem item,
            IDictionary<string, string> imageUrlCache,
            Func<Task> stateHasChangedCallback,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Manual mock implementation for IImagePlaceholderService since LightMock.Generator doesn't support it.
    /// </summary>
    public sealed class ImagePlaceholderService_Mock : IImagePlaceholderService
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

        public void SetupImageUrl(string? itemLink, string resultUrl)
        {
            var key = itemLink ?? "null-link";
            _imageUrls[key] = resultUrl;
        }

        public void SetupFallbackStatus(string? itemLink, bool hasFallback)
        {
            var key = itemLink ?? "null-link";
            _fallbackStatuses[key] = hasFallback;
        }

        public void SetupFallbackReason(string? itemLink, string reason)
        {
            var key = itemLink ?? "null-link";
            _fallbackReasons[key] = reason;
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
            var key = item.Link ?? item.Id.ToString();
            return _imageUrls.TryGetValue(key, out var url) ? url : imageUrlCache.TryGetValue(key, out var cachedUrl) ? cachedUrl : _defaultPlaceholder;
        }

        public Task HandleImageLoadAsync(
            string elementId,
            string itemLink,
            bool loadSuccess,
            IDictionary<string, string> imageUrlCache,
            Func<string, Task> stopShimmerAsync,
            Func<Task> stateHasChangedCallback)
        {
            return Task.CompletedTask;
        }

        public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            var key = item.Link ?? item.Id.ToString();
            return _fallbackStatuses.TryGetValue(key, out var status) && status;
        }

        public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            var key = item.Link ?? item.Id.ToString();
            return _fallbackReasons.TryGetValue(key, out var reason) ? reason : string.Empty;
        }
    }

    /// <summary>
    ///     Manual IMediator mock for Videos page load/refresh use cases.
    /// </summary>
    public sealed class RaindropMediator_Mock : IMediator
    {
        private Result<RaindropItemsResponse> _loadResult =
            Result.Success(new RaindropItemsResponse([], IsFromCache: false, HasUpdateAvailable: false));

        private Result<RaindropItemsResponse> _refreshResult =
            Result.Success(new RaindropItemsResponse([], IsFromCache: false, HasUpdateAvailable: false));

        private string? _refreshFailure;

        public void SetupLoad(IReadOnlyList<RaindropItem> items, bool isFromCache = false)
        {
            _loadResult = Result.Success(new RaindropItemsResponse(items.ToList(), isFromCache, HasUpdateAvailable: false));
        }

        public void SetupLoadFailure(string error = "Simulated API failure")
        {
            _loadResult = Result.Failure<RaindropItemsResponse>(error);
        }

        public void SetupRefresh(IReadOnlyList<RaindropItem> items)
        {
            _refreshFailure = null;
            _refreshResult = Result.Success(new RaindropItemsResponse(items.ToList(), IsFromCache: false, HasUpdateAvailable: false));
        }

        public void SetupRefreshFailure(string error = "Simulated API failure")
        {
            _refreshFailure = error;
        }

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is LoadArticlesQuery or LoadVideosQuery)
                return ValueTask.FromResult((TResponse)(object)_loadResult);

            if (request is RefreshArticlesCommand or RefreshVideosCommand)
            {
                // Double-refresh is gated by the page (_context.IsRefreshing), not wall-clock delay.
                if (_refreshFailure is not null)
                    return ValueTask.FromResult((TResponse)(object)Result.Failure<RaindropItemsResponse>(_refreshFailure));

                return ValueTask.FromResult((TResponse)(object)_refreshResult);
            }

            throw new InvalidOperationException($"Unexpected request type: {request.GetType().Name}");
        }

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
