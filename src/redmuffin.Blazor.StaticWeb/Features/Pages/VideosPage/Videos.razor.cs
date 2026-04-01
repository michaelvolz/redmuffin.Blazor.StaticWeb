using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Cache.Enums;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    private const string CacheKey = "Videos";
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.Ordinal);
    private string? _errorMessage;
    private List<RaindropItem>? _videoItems;
    private RefreshBadgeState _refreshBadgeState = RefreshBadgeState.Hidden;
    private bool _isRefreshing;

    [Inject]
    private ILogger<Videos> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IImageValidationCacheService ImageValidationCacheService { get; set; } = null!;

    [Inject]
    private IRaindropItemsCache RaindropItemsCache { get; set; } = null!;

    private static string DisplayTitle(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Title) ? "No Title Available" : video.Title;
    }

    private static string DisplayExcerpt(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Excerpt)
            ? "No Excerpt Available"
            : video.Excerpt.Length > 250
                ? string.Concat(video.Excerpt.AsSpan(0, 250), "...")
                : video.Excerpt;
    }

    private static bool HasDataChanged(List<RaindropItem> currentData, List<RaindropItem> newData)
    {
        if (currentData.Count != newData.Count) return true;

        // Simple comparison - check if any items have different titles or links
        for (var i = 0; i < currentData.Count; i++)
            if (!string.Equals(currentData[i].Link, newData[i].Link, StringComparison.Ordinal) ||
                !string.Equals(currentData[i].Title, newData[i].Title, StringComparison.Ordinal))
                return true;

        return false;
    }

    protected override async Task OnInitializedAsync()
    {
        // Validate injected dependencies
#pragma warning disable MA0015 // Not method parameters — validating Blazor [Inject] properties
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
        ArgumentNullException.ThrowIfNull(ImageValidationCacheService);
        ArgumentNullException.ThrowIfNull(RaindropItemsCache);
#pragma warning restore MA0015

        // Load cached data first for immediate display
        await LoadCachedDataAsync().ConfigureAwait(false);
        StateHasChanged();

        // Fetch fresh data in background
        _ = Task.Run(async () => await RefreshDataInBackgroundAsync().ConfigureAwait(false));
    }

    // Update RainDropClientId based on environment
    private string GetRainDropClientId()
    {
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return baseUri.Contains("localhost") ? "684ea82bb3333b01de5487c1" : "684c73df642469e7c1969f8e";
    }

    private async Task LoginWithRaindropAsync()
    {
        var redirectPath = "/redirect";
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        var redirectUri = $"{baseUri}{redirectPath}";
        var authUrl =
            $"https://raindrop.io/oauth/authorize?client_id={GetRainDropClientId()}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code";

        await Js.InvokeVoidAsync("open", authUrl, "_self").ConfigureAwait(false);
    }

    private async Task LoadCachedDataAsync()
    {
        try
        {
            var cacheResult = await RaindropItemsCache.GetAsync(CacheKey, CancellationToken.None).ConfigureAwait(false);

            if (cacheResult.IsSuccess && cacheResult.Data != null)
            {
                _videoItems = cacheResult.Data.ToList();

                // Populate image cache for cached videos
                if (_videoItems.Count > 0)
                    try
                    {
                        await ImageValidationCacheService.PopulateImageUrlCacheAsync(
                            _videoItems,
                            _imageUrlCache,
                            () => InvokeAsync(StateHasChanged),
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception imageEx)
                    {
                        LogImageCacheWarningCached(Logger, imageEx);
                        // Continue without image cache - videos will still display
                    }

                LogVideosLoadedFromCache(Logger, _videoItems.Count);
            }
            else
            {
                // No cache available, fetch fresh data immediately
                LogNoCachedData(Logger);
                await FetchVideosAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogCacheLoadError(Logger, ex);
            try
            {
                await FetchVideosAsync().ConfigureAwait(false);
            }
            catch (Exception fetchEx)
            {
                LogFetchError(Logger, fetchEx);
                _errorMessage = "Unable to load videos. Please check your internet connection and try refreshing the page.";
                StateHasChanged();
            }
        }
    }

    private async Task RefreshDataInBackgroundAsync()
    {
        try
        {
            var freshVideos = await RaindropAPI.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
            var freshVideosList = freshVideos.ToList();

            // Check if we have newer data
            if (_videoItems == null || HasDataChanged(_videoItems, freshVideosList))
            {
                try
                {
                    // Cache the fresh data
                    await RaindropItemsCache.SetAsync(CacheKey, freshVideosList, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cacheEx)
                {
                    LogCacheStoreError(Logger, cacheEx);
                }

                // Show refresh badge if we already have data displayed
                if (_videoItems != null && _videoItems.Count > 0)
                {
                    _refreshBadgeState = RefreshBadgeState.Visible;
                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                }
                else
                {
                    // No existing data, update immediately
                    _videoItems = freshVideosList;
                    try
                    {
                        await PopulateImageCacheAsync().ConfigureAwait(false);
                    }
                    catch (Exception imageEx)
                    {
                        LogImageCacheError(Logger, imageEx);
                    }

                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            LogNetworkErrorBackground(Logger, httpEx);
            // Don't show error to user for background operations
        }
        catch (TaskCanceledException timeoutEx)
        {
            LogTimeoutBackground(Logger, timeoutEx);
            // Don't show error to user for background operations
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorBackground(Logger, ex);
            // Don't show error to user for background operations
        }
    }

    private async Task FetchVideosAsync()
    {
        _errorMessage = null;
        _videoItems = null;
        try
        {
            var videos = await RaindropAPI.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
            _videoItems = videos.ToList();

            // Cache the fresh data
            await RaindropItemsCache.SetAsync(CacheKey, _videoItems, CancellationToken.None).ConfigureAwait(false);

            await PopulateImageCacheAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Exception fetching videos: {ex.Message}";
        }

        StateHasChanged();
    }

    private async Task PopulateImageCacheAsync()
    {
        // Populate image cache for videos
        if (_videoItems != null && _videoItems.Count > 0)
            await ImageValidationCacheService.PopulateImageUrlCacheAsync(
                _videoItems,
                _imageUrlCache,
                () => InvokeAsync(StateHasChanged),
                CancellationToken.None).ConfigureAwait(false);
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
            var freshVideos = await RaindropAPI.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
            _videoItems = freshVideos.ToList();

            // Try to cache the fresh data, but don't fail if caching fails
            try
            {
                await RaindropItemsCache.SetAsync(CacheKey, _videoItems, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cacheEx)
            {
                LogCacheRefreshError(Logger, cacheEx);
            }

            // Try to populate image cache, but don't fail if it fails
            try
            {
                await PopulateImageCacheAsync().ConfigureAwait(false);
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
            _errorMessage = "Unable to refresh videos. Please check your internet connection and try again.";
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
            _errorMessage = "An unexpected error occurred while refreshing videos. Please try again.";
            _refreshBadgeState = RefreshBadgeState.Error;
        }
        finally
        {
            _isRefreshing = false;
            StateHasChanged();
        }
    }

    private string GetDefaultPlaceholder()
    {
        return ImagePlaceholderService.GetDefaultPlaceholder();
    }

    private string GetImageUrl(RaindropItem video)
    {
        return ImagePlaceholderService.GetImageUrl(video, _imageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string videoLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            videoLink,
            loadSuccess,
            _imageUrlCache,
            Js,
            () => InvokeAsync(StateHasChanged));
    }

    private bool HasFallbackPlaceholder(RaindropItem video)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(video, _imageUrlCache);
    }

    private string GetFallbackReason(RaindropItem video)
    {
        return ImagePlaceholderService.GetFallbackReason(video, _imageUrlCache);
    }
}