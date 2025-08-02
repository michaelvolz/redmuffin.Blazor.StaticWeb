using Bunit;
using LightMock.Generator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Pages.ArticlesPage;

/// <summary>
///     Helper methods and infrastructure for ArticlesPageCacheTests.
/// </summary>
public partial class ArticlesPageCacheTests
{
    /// <summary>
    ///     Creates a new test scope with standard configuration.
    /// </summary>
    /// <returns>A configured test scope ready for component testing.</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    /// <summary>
    ///     Creates a test RaindropItem for testing purposes.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <param name="title">The item title.</param>
    /// <param name="excerpt">The item excerpt.</param>
    /// <param name="link">The item link (optional).</param>
    /// <returns>A configured test RaindropItem.</returns>
    private static RaindropItem CreateTestArticle(string id, string title, string excerpt, string? link = null)
    {
        return new RaindropItem
        {
            Id = long.Parse(id),
            Title = title,
            Excerpt = excerpt,
            Link = link ?? $"https://example.com/article/{id}",
            Cover = $"https://example.com/cover/{id}.jpg",
            Created = DateTime.UtcNow.AddDays(-1)
        };
    }

    /// <summary>
    ///     Test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        /// <summary>
        ///     Gets the bUnit test context for component rendering.
        /// </summary>
        public BunitContext Context { get; } = new();

        /// <summary>
        ///     Gets the mock for IRaindropItemsCache service.
        /// </summary>
        public CacheService_Mock CacheService_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock for IRaindropAPI service.
        /// </summary>
        public RaindropAPI_Mock RaindropAPI_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock for IImagePlaceholderService.
        /// </summary>
        public ImagePlaceholderService_Mock ImagePlaceholderService_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock for IImageValidationCacheService.
        /// </summary>
        public ImageValidationCacheService_Mock ImageValidationCacheService_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock logger for Articles component (external dependency - uses LightMock).
        /// </summary>
        public Mock<ILogger<Articles>> Logger_Mock { get; } = new();

        /// <summary>
        ///     Configures the test scope with standard services for component testing.
        /// </summary>
        /// <returns>The configured test scope for method chaining.</returns>
        public TestScope WithStandardServices()
        {
            // Register mocked services
            Context.Services.AddSingleton<IRaindropItemsCache>(CacheService_Mock);
            Context.Services.AddSingleton<IRaindropAPI>(RaindropAPI_Mock);
            Context.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService_Mock);
            Context.Services.AddSingleton<IImageValidationCacheService>(ImageValidationCacheService_Mock);
            Context.Services.AddSingleton(Logger_Mock.Object);

            return this;
        }

        /// <summary>
        ///     Disposes of test resources.
        /// </summary>
        public void Dispose()
        {
            Context?.Dispose();
        }
    }

    /// <summary>
    ///     Custom mock for IRaindropItemsCache to simulate caching behavior.
    /// </summary>
    public sealed class CacheService_Mock : IRaindropItemsCache
    {
        private readonly Dictionary<string, List<RaindropItem>> _cache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _cacheFailures = new(StringComparer.Ordinal);

        public void SetupCachedData(string key, IEnumerable<RaindropItem> data)
        {
            _cache[key] = data.ToList();
        }

        public void SetupNoCachedData(string key)
        {
            _cache.Remove(key);
        }

        public void SetupCacheFailure(string key)
        {
            _cacheFailures[key] = true;
        }

        public Task<RaindropCacheResult<IList<RaindropItem>>> GetAsync(string cacheType, CancellationToken cancellationToken = default)
        {
            if (_cacheFailures.ContainsKey(cacheType)) throw new InvalidOperationException("Cache failure simulation");

            if (_cache.TryGetValue(cacheType, out var data))
                return Task.FromResult(RaindropCacheResultFactory.Success(data as IList<RaindropItem>, new RaindropCacheMetadata
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    ItemCount = data.Count,
                    CompressedSize = 1000,
                    OriginalSize = 2000
                }));

            return Task.FromResult(RaindropCacheResultFactory.Miss<IList<RaindropItem>>());
        }

        public Task SetAsync(string cacheType, IList<RaindropItem> items, CancellationToken cancellationToken = default)
        {
            _cache[cacheType] = items.ToList();
            return Task.CompletedTask;
        }

        public Task<bool> IsExpiredAsync(string cacheType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task ClearAsync(string cacheType, CancellationToken cancellationToken = default)
        {
            _cache.Remove(cacheType);
            return Task.CompletedTask;
        }

        public Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Custom mock for IRaindropAPI to simulate API behavior.
    /// </summary>
    public sealed class RaindropAPI_Mock : IRaindropAPI
    {
        private List<RaindropItem> _articles = new();
        private string? _failureMessage;
        private int _delayMs;
        private bool _preventDoubleRefresh;
        private bool _refreshInProgress;

        public void SetupArticles(IEnumerable<RaindropItem> articles)
        {
            _articles = articles.ToList();
            _failureMessage = null;
        }

        public void SetupFailure(string message)
        {
            _failureMessage = message;
        }

        public void SetupDelay(int milliseconds)
        {
            _delayMs = milliseconds;
        }

        public void SetupDoubleRefreshPrevention(bool prevent)
        {
            _preventDoubleRefresh = prevent;
        }

        public async Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            if (_preventDoubleRefresh && _refreshInProgress) throw new InvalidOperationException("Double refresh prevented");

            _refreshInProgress = true;

            try
            {
                if (_delayMs > 0) await Task.Delay(_delayMs, cancellationToken).ConfigureAwait(false);

                if (_failureMessage != null) throw new HttpRequestException(_failureMessage);

                return _articles;
            }
            finally
            {
                _refreshInProgress = false;
            }
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Videos not needed for Articles page tests");
        }
    }

    /// <summary>
    ///     Custom mock for IImagePlaceholderService to simulate image placeholder behavior.
    /// </summary>
    public sealed class ImagePlaceholderService_Mock : IImagePlaceholderService
    {
        public string GetDefaultPlaceholder()
        {
            return
                "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMzAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZGRkIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzk5OSIgZG9taW5hbnQtYmFzZWxpbmU9Im1pZGRsZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+UGxhY2Vob2xkZXI8L3RleHQ+PC9zdmc+";
        }

        public string GenerateSimplePlaceholder(string reason)
        {
            return
                "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMzAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZGRkIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxNCIgZmlsbD0iIzk5OSIgZG9taW5hbnQtYmFzZWxpbmU9Im1pZGRsZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI>{reason}</dGV4dD48L3N2Zz4=";
        }

        public string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return item.Cover ?? "default-placeholder.svg";
        }

        public Task HandleImageLoadAsync(string elementId, string itemLink, bool loadSuccess, IDictionary<string, string> imageUrlCache, IJSRuntime jsRuntime,
            Func<Task> stateHasChangedCallback)
        {
            return Task.CompletedTask;
        }

        public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return false;
        }

        public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     Custom mock for IImageValidationCacheService to simulate image validation behavior.
    /// </summary>
    public sealed class ImageValidationCacheService_Mock : IImageValidationCacheService
    {
        public Task PopulateImageUrlCacheAsync(IEnumerable<RaindropItem> items, IDictionary<string, string> imageUrlCache, Func<Task> stateHasChangedCallback,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetCachedImageUrlAsync(RaindropItem item, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(item.Cover ?? "default-placeholder.svg");
        }

        public Task ValidateImageInBackgroundAsync(RaindropItem item, IDictionary<string, string> imageUrlCache, Func<Task> stateHasChangedCallback,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
