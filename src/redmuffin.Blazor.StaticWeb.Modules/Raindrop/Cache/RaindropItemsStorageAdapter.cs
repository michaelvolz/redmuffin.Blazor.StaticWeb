using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Cache;

/// <summary>
///     Host adapter: maps module storage port to LocalStorage-backed <see cref="IRaindropItemsCache" />.
/// </summary>
public sealed class RaindropItemsStorageAdapter(IRaindropItemsCache cache) : IRaindropItemsStorage
{
    private readonly IRaindropItemsCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    /// <inheritdoc />
    public async Task<IReadOnlyList<RaindropItem>?> TryGetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var result = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
            return result.Data is IReadOnlyList<RaindropItem> list ? list : result.Data.ToList();

        return null;
    }

    /// <inheritdoc />
    public Task SetAsync(
        string cacheKey,
        IReadOnlyList<RaindropItem> items,
        CancellationToken cancellationToken = default)
    {
        IList<RaindropItem> mutable = items is IList<RaindropItem> list
            ? list
            : items.ToList();
        return _cache.SetAsync(cacheKey, mutable, cancellationToken);
    }
}
