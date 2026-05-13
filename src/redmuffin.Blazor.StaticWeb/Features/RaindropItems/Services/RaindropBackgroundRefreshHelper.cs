using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;

/// <summary>
///     Shared background refresh logic used by Videos and Articles pages.
///     Encapsulates the fetch-cache-compare pipeline so components only
///     handle UI decisions (show badge vs update immediately).
/// </summary>
public static class RaindropBackgroundRefreshHelper
{
    /// <summary>
    ///     Determines whether two lists of raindrop items have different data.
    /// </summary>
    public static bool HasDataChanged(IReadOnlyList<RaindropItem> currentData, IReadOnlyList<RaindropItem> newData)
    {
        if (currentData.Count != newData.Count) return true;

        for (var i = 0; i < currentData.Count; i++)
            if (!string.Equals(currentData[i].Link, newData[i].Link, StringComparison.Ordinal) ||
                !string.Equals(currentData[i].Title, newData[i].Title, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    ///     Fetches fresh data from the API and caches it if changed.
    ///     Returns the fresh data list, or null if nothing changed.
    /// </summary>
    public static async Task<ICollection<RaindropItem>?> TryFetchFreshDataAsync(
        Func<Task<IEnumerable<RaindropItem>>> fetchAsync,
        IReadOnlyList<RaindropItem>? currentItems,
        Func<IReadOnlyList<RaindropItem>, CancellationToken, Task> cacheAsync,
        CancellationToken cancellationToken)
    {
        var freshItems = (await fetchAsync().ConfigureAwait(false)).ToList();

        if (currentItems != null && !HasDataChanged(currentItems, freshItems))
            return null;

        await cacheAsync(freshItems, cancellationToken).ConfigureAwait(false);
        return freshItems;
    }
}
