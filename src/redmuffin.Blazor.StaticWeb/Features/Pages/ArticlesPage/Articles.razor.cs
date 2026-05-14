using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Cache.Enums;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;
using static redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services.RaindropItemPresentationHelper;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

public partial class Articles
{
    private const string CacheKey = "Articles";

    // LoggerMessage delegates for performance
    private static readonly Action<ILogger, Exception?> LogImageCacheWarning =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, "ImageCacheWarning"),
            "Failed to populate image cache for cached articles, images may load slower");

    private static readonly Action<ILogger, int, Exception?> LogArticlesLoaded =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, "ArticlesLoaded"), "Loaded {Count} articles from cache");

    private static readonly Action<ILogger, Exception?> LogNoCachedData =
        LoggerMessage.Define(LogLevel.Information, new EventId(3, "NoCachedData"), "No cached article data available, fetching fresh data");

    private static readonly Action<ILogger, Exception?> LogCacheLoadError =
        LoggerMessage.Define(LogLevel.Warning, new EventId(4, "CacheLoadError"), "Error loading cached articles, falling back to fresh data");

    private static readonly Action<ILogger, Exception?> LogFetchError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5, "FetchError"), "Failed to fetch fresh articles after cache failure");

    private static readonly Action<ILogger, Exception?> LogImageCacheError =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7, "ImageCacheError"), "Failed to populate image cache, continuing with basic display");

    private static readonly Action<ILogger, Exception?> LogNetworkErrorBackground =
        LoggerMessage.Define(LogLevel.Warning, new EventId(8, "NetworkErrorBackground"), "Network error during background refresh, will retry later");

    private static readonly Action<ILogger, Exception?> LogTimeoutBackground =
        LoggerMessage.Define(LogLevel.Warning, new EventId(9, "TimeoutBackground"), "Request timeout during background refresh, will retry later");

    private static readonly Action<ILogger, Exception?> LogUnexpectedErrorBackground =
        LoggerMessage.Define(LogLevel.Error, new EventId(10, "UnexpectedErrorBackground"), "Unexpected error refreshing articles in background");

    private static readonly Action<ILogger, Exception?> LogCacheRefreshError =
        LoggerMessage.Define(LogLevel.Warning, new EventId(11, "CacheRefreshError"), "Failed to cache refreshed article data, data will still be displayed");

    private static readonly Action<ILogger, Exception?> LogImageCacheRefreshError =
        LoggerMessage.Define(LogLevel.Warning, new EventId(12, "ImageCacheRefreshError"),
            "Failed to populate image cache during refresh, images may load slower");

    private static readonly Action<ILogger, Exception?> LogNetworkErrorManual =
        LoggerMessage.Define(LogLevel.Error, new EventId(13, "NetworkErrorManual"), "Network error during manual refresh");

    private static readonly Action<ILogger, Exception?> LogTimeoutManual =
        LoggerMessage.Define(LogLevel.Error, new EventId(14, "TimeoutManual"), "Request timeout during manual refresh");

    private static readonly Action<ILogger, Exception?> LogUnexpectedErrorManual =
        LoggerMessage.Define(LogLevel.Error, new EventId(15, "UnexpectedErrorManual"), "Unexpected error during manual refresh");

    // Simple state management - only what we need
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.OrdinalIgnoreCase);
    private List<RaindropItem>? _articleItems;

    private string? _errorMessage;
    private bool _isLoading;
    private RefreshBadgeState _refreshBadgeState = RefreshBadgeState.Hidden;
    private bool _isRefreshing;

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IImageValidationCacheService ImageValidationCacheService { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    [Inject]
    private IRaindropItemsCache RaindropItemsCache { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Validate injected dependencies
#pragma warning disable MA0015 // Not method parameters — validating Blazor [Inject] properties
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
        ArgumentNullException.ThrowIfNull(ImageValidationCacheService);
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(RaindropItemsCache);
#pragma warning restore MA0015

        // Load cached data first for immediate display
        await LoadCachedDataAsync().ConfigureAwait(false);
        StateHasChanged();

        // Fetch fresh data in background
        _ = Task.Run(async () => await RefreshDataInBackgroundAsync().ConfigureAwait(false));
    }

    protected string GetFallbackReason(RaindropItem article)
    {
        return ImagePlaceholderService.GetFallbackReason(article, _imageUrlCache);
    }

    private async Task LoadCachedDataAsync()
    {
        try
        {
            var cacheResult = await RaindropItemsCache.GetAsync(CacheKey, CancellationToken.None).ConfigureAwait(false);

            if (cacheResult.IsSuccess && cacheResult.Data != null)
            {
                _articleItems = cacheResult.Data.ToList();

                // Populate image cache for cached articles
                if (_articleItems.Count > 0)
                    try
                    {
                        await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                    }
                    catch (Exception imageEx)
                    {
                        LogImageCacheWarning(Logger, imageEx);
                        // Continue without image cache - articles will still display
                    }

                LogArticlesLoaded(Logger, _articleItems.Count, null);
            }
            else
            {
                // No cache available, fetch fresh data immediately
                LogNoCachedData(Logger, null);
                await FetchArticlesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogCacheLoadError(Logger, ex);
            try
            {
                await FetchArticlesAsync().ConfigureAwait(false);
            }
            catch (Exception fetchEx)
            {
                LogFetchError(Logger, fetchEx);
                _errorMessage = "Unable to load articles. Please check your internet connection and try refreshing the page.";
                StateHasChanged();
            }
        }
    }

    private async Task RefreshDataInBackgroundAsync()
    {
        try
        {
            var freshItems = await RaindropBackgroundRefreshHelper.TryFetchFreshDataAsync(
                () => RaindropAPI.GetArticlesAsync(CancellationToken.None),
                _articleItems,
                (data, ct) => RaindropItemsCache.SetAsync(CacheKey, (List<RaindropItem>)data, ct),
                CancellationToken.None).ConfigureAwait(false);

            if (freshItems == null) return;

            if (_articleItems is { Count: > 0 })
            {
                _refreshBadgeState = RefreshBadgeState.Visible;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
            else
            {
                _articleItems = freshItems.ToList();
                try
                {
                    await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                }
                catch (Exception imageEx)
                {
                    LogImageCacheError(Logger, imageEx);
                }

                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException httpEx)
        {
            LogNetworkErrorBackground(Logger, httpEx);
        }
        catch (TaskCanceledException timeoutEx)
        {
            LogTimeoutBackground(Logger, timeoutEx);
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorBackground(Logger, ex);
        }
    }

    private async Task FetchArticlesAsync()
    {
        _errorMessage = null;
        _articleItems = null;
        _isLoading = true;

        // Clear image cache when fetching new articles
        _imageUrlCache.Clear();

        StateHasChanged();

        try
        {
            var articles = await RaindropAPI.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);
            _articleItems = articles.ToList();

            // Cache the fresh data
            await RaindropItemsCache.SetAsync(CacheKey, _articleItems, CancellationToken.None).ConfigureAwait(false);

            // Populate image URL cache for initial render
            if (_articleItems is { Count: > 0 })
            {
                await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Exception fetching articles: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }

        StateHasChanged();
    }

    private async Task HandleRefreshClickAsync()
    {
        if (_isRefreshing) return;

        _isRefreshing = true;
        _refreshBadgeState = RefreshBadgeState.Loading;
        _errorMessage = null; // Clear any previous errors
        StateHasChanged();

        try
        {
            var freshArticles = await RaindropAPI.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);
            _articleItems = freshArticles.ToList();

            // Try to cache the fresh data, but don't fail if caching fails
            try
            {
                await RaindropItemsCache.SetAsync(CacheKey, _articleItems, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cacheEx)
            {
                LogCacheRefreshError(Logger, cacheEx);
            }

            // Try to populate image cache, but don't fail if it fails
            try
            {
                await PopulateImageUrlCacheAsync().ConfigureAwait(false);
            }
            catch (Exception imageEx)
            {
                LogImageCacheRefreshError(Logger, imageEx);
            }

            _refreshBadgeState = RefreshBadgeState.Hidden;
        }
        catch (HttpRequestException httpEx)
        {
            LogNetworkErrorManual(Logger, httpEx);
            _errorMessage = "Unable to refresh articles. Please check your internet connection and try again.";
            _refreshBadgeState = RefreshBadgeState.Error;
        }
        catch (TaskCanceledException timeoutEx)
        {
            LogTimeoutManual(Logger, timeoutEx);
            _errorMessage = "Request timed out. Please try again.";
            _refreshBadgeState = RefreshBadgeState.Error;
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorManual(Logger, ex);
            _errorMessage = "An unexpected error occurred while refreshing articles. Please try again.";
            _refreshBadgeState = RefreshBadgeState.Error;
        }
        finally
        {
            _isRefreshing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Populates the image URL cache for all articles using ONLY cached values.
    ///     This method never triggers network requests, ensuring fast page loads.
    ///     Background validation is started for uncached images.
    /// </summary>
    private async Task PopulateImageUrlCacheAsync()
    {
        if (_articleItems == null) return;

        await ImageValidationCacheService.PopulateImageUrlCacheAsync(
            _articleItems,
            _imageUrlCache,
            () => InvokeAsync(StateHasChanged),
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the image URL for an article from the cache.
    ///     This method is used by the UI for rendering.
    /// </summary>
    /// <param name="article">The article to get the image URL for</param>
    /// <returns>The cached image URL or a default placeholder</returns>
    private string GetImageUrl(RaindropItem article)
    {
        return ImagePlaceholderService.GetImageUrl(article, _imageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string articleLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            articleLink,
            loadSuccess,
            _imageUrlCache,
            Js,
            () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(article, _imageUrlCache);
    }
}