using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;

/// <summary>
///     Pure orchestration for Raindrop page lifecycle — cache priming,
///     background refresh, manual refresh, and image cache population.
///     No state of its own. Each method takes the context and callbacks it needs.
/// </summary>
/// <remarks>
///     Source-generated LoggerMessage delegates in partial class file:
///     RaindropPageOrchestrator.Logging.cs
/// </remarks>
public static partial class RaindropPageOrchestrator
{
    public static async Task LoadCachedDataAsync(
        RaindropPageContext ctx,
        string cacheKey,
        IRaindropItemsCache cache,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        Func<Task> populateImagesAsync,
        ILogger logger)
    {
        try
        {
            var cacheResult = await cache.GetAsync(cacheKey, CancellationToken.None).ConfigureAwait(false);

            if (cacheResult.IsSuccess && cacheResult.Data != null)
            {
                ctx.Items = cacheResult.Data.ToList();

                await PopulateImagesIfItemsAsync(ctx, populateImagesAsync, logger).ConfigureAwait(false);
            }
            else
            {
                await FetchAndCacheAsync(ctx, cacheKey, fetchAsync, cache, populateImagesAsync, logger).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogCacheLoadFallback(logger, ex);
            try
            {
                await FetchAndCacheAsync(ctx, cacheKey, fetchAsync, cache, populateImagesAsync, logger).ConfigureAwait(false);
            }
            catch (Exception fetchEx)
            {
                LogFreshFetchFailure(logger, fetchEx);
                ctx.ErrorMessage = "Unable to load items. Please check your internet connection and try refreshing the page.";
            }
        }
    }

    private static async Task PopulateImagesIfItemsAsync(
        RaindropPageContext ctx,
        Func<Task> populateImagesAsync,
        ILogger logger)
    {
        if (ctx.Items is not { Count: > 0 })
            return;

        try
        {
            await populateImagesAsync().ConfigureAwait(false);
        }
        catch (Exception imageEx)
        {
            LogImageCacheWarning(logger, imageEx);
        }
    }

    public static async Task HandleRefreshClickAsync(
        RaindropPageContext ctx,
        string cacheKey,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        IRaindropItemsCache cache,
        Func<Task> populateImagesAsync,
        Func<Task> stateHasChangedAsync,
        ILogger logger)
    {
        if (ctx.IsRefreshing)
            return;

        ctx.IsRefreshing = true;
        ctx.BadgeState = RefreshBadgeState.Loading;
        ctx.ErrorMessage = null;
        await stateHasChangedAsync().ConfigureAwait(false);

        try
        {
            var freshItems = (await fetchAsync(CancellationToken.None).ConfigureAwait(false)).ToList();
            ctx.Items = freshItems;

            try
            {
                await cache.SetAsync(cacheKey, freshItems, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cacheEx)
            {
                LogCacheRefreshWarning(logger, cacheEx);
            }

            await PopulateImagesIfItemsAsync(ctx, populateImagesAsync, logger).ConfigureAwait(false);

            ctx.BadgeState = RefreshBadgeState.Hidden;
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException)
            {
                LogNetworkRefreshError(logger, ex);
                ctx.ErrorMessage = "Unable to refresh. Please check your internet connection and try again.";
            }
            else if (ex is TaskCanceledException)
            {
                LogRefreshTimeout(logger, ex);
                ctx.ErrorMessage = "Request timed out. Please try again.";
            }
            else
            {
                LogUnexpectedRefreshError(logger, ex);
                ctx.ErrorMessage = "An unexpected error occurred while refreshing. Please try again.";
            }

            ctx.BadgeState = RefreshBadgeState.Error;
        }
        finally
        {
            ctx.IsRefreshing = false;
            await stateHasChangedAsync().ConfigureAwait(false);
        }
    }

    public static async Task FetchAndCacheAsync(
        RaindropPageContext ctx,
        string cacheKey,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        IRaindropItemsCache cache,
        Func<Task> populateImagesAsync,
        ILogger logger)
    {
        ctx.ErrorMessage = null;
        ctx.Items = null;

        ctx.ImageUrlCache.Clear();

        try
        {
            var items = (await fetchAsync(CancellationToken.None).ConfigureAwait(false)).ToList();
            ctx.Items = items;

            await cache.SetAsync(cacheKey, items, CancellationToken.None).ConfigureAwait(false);

            if (items is { Count: > 0 })
            {
                await populateImagesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogFetchAndCacheFailure(logger, ex);
            ctx.ErrorMessage = "Unable to load items. Please check your internet connection and try refreshing the page.";
        }
    }

    public static async Task RefreshInBackgroundAsync(
        RaindropPageContext ctx,
        string cacheKey,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        IRaindropItemsCache cache,
        Func<Task> populateImagesAsync,
        Func<Task> stateHasChangedAsync,
        ILogger logger)
    {
        try
        {
            var freshItems = await RaindropBackgroundRefreshHelper.TryFetchFreshDataAsync(
                () => fetchAsync(CancellationToken.None),
                ctx.Items,
                (data, ct) => cache.SetAsync(cacheKey, (List<RaindropItem>)data, ct),
                CancellationToken.None).ConfigureAwait(false);

            if (freshItems == null)
                return;

            if (ctx.Items is { Count: > 0 })
            {
                ctx.BadgeState = RefreshBadgeState.Visible;
            }
            else
            {
                ctx.Items = freshItems.ToList();
                await PopulateImagesIfItemsAsync(ctx, populateImagesAsync, logger).ConfigureAwait(false);
            }

            await stateHasChangedAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException httpEx)
        {
            LogNetworkRefreshError(logger, httpEx);
        }
        catch (TaskCanceledException timeoutEx)
        {
            LogRefreshTimeout(logger, timeoutEx);
        }
        catch (Exception ex)
        {
            LogUnexpectedRefreshError(logger, ex);
        }
    }
}
