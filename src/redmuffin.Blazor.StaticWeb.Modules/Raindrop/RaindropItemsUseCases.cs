using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Shared cache-first load and force-refresh policy for Raindrop item lists.
/// </summary>
internal static class RaindropItemsUseCases
{
    public const string ArticlesCacheKey = "Articles";
    public const string VideosCacheKey = "Videos";

    public static async Task<Result<RaindropItemsResponse>> LoadAsync(
        IRaindropItemsStorage storage,
        Func<CancellationToken, Task<Result<IReadOnlyList<RaindropItem>>>> fetchAsync,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(fetchAsync);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        var cached = await storage.TryGetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return Result.Success(new RaindropItemsResponse(cached, IsFromCache: true, HasUpdateAvailable: false));

        return await FetchAndCacheAsync(storage, fetchAsync, cacheKey, cancellationToken).ConfigureAwait(false);
    }

    public static Task<Result<RaindropItemsResponse>> RefreshAsync(
        IRaindropItemsStorage storage,
        Func<CancellationToken, Task<Result<IReadOnlyList<RaindropItem>>>> fetchAsync,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(fetchAsync);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        return FetchAndCacheAsync(storage, fetchAsync, cacheKey, cancellationToken);
    }

    private static async Task<Result<RaindropItemsResponse>> FetchAndCacheAsync(
        IRaindropItemsStorage storage,
        Func<CancellationToken, Task<Result<IReadOnlyList<RaindropItem>>>> fetchAsync,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var fetchResult = await fetchAsync(cancellationToken).ConfigureAwait(false);
        if (fetchResult.IsFailure)
            return Result.Failure<RaindropItemsResponse>(fetchResult.Error);

        var items = fetchResult.Value;
        try
        {
            await storage.SetAsync(cacheKey, items, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Cache write failure must not fail a successful fetch (same as host orchestrator).
        }

        return Result.Success(new RaindropItemsResponse(items, IsFromCache: false, HasUpdateAvailable: false));
    }
}
