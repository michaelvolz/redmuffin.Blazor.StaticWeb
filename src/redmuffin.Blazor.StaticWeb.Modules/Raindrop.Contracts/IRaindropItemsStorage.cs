using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Module port for Raindrop item list storage. Concrete browser APIs stay out of Contracts.
/// </summary>
public interface IRaindropItemsStorage
{
    /// <summary>
    ///     Attempts to read cached items for the given key.
    /// </summary>
    /// <returns>Cached items on hit; null on miss, expiry, or unreadable cache.</returns>
    Task<IReadOnlyList<RaindropItem>?> TryGetAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Writes items for the given cache key.
    /// </summary>
    Task SetAsync(string cacheKey, IReadOnlyList<RaindropItem> items, CancellationToken cancellationToken = default);
}
