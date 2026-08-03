using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Cache;

/// <summary>
///     Interface for caching raindrop items in LocalStorage with compression and expiration support.
/// </summary>
public interface IRaindropItemsCache
{
    /// <summary>
    ///     Retrieves cached raindrop items for the specified cache type.
    /// </summary>
    /// <param name="cacheType">The type of cache (Videos or Articles).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A cache result containing the cached items or null if not found/expired.</returns>
    Task<RaindropCacheResult<IList<RaindropItem>>> GetAsync(string cacheType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores raindrop items in cache with compression and metadata.
    /// </summary>
    /// <param name="cacheType">The type of cache (Videos or Articles).</param>
    /// <param name="items">The raindrop items to cache.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(string cacheType, IList<RaindropItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears the cache for the specified cache type.
    /// </summary>
    /// <param name="cacheType">The type of cache to clear (Videos or Articles).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync(string cacheType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if the cache for the specified type has expired.
    /// </summary>
    /// <param name="cacheType">The type of cache to check (Videos or Articles).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the cache is expired or doesn't exist, false otherwise.</returns>
    Task<bool> IsExpiredAsync(string cacheType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all cached data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}