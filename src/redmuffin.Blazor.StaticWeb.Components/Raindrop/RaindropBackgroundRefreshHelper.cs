using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Components.Raindrop;

/// <summary>
///     Shared helpers for Raindrop progressive refresh UI decisions.
///     Fetch/cache policy lives in Mediator handlers; pages only compare lists for the badge.
/// </summary>
public static class RaindropBackgroundRefreshHelper
{
    /// <summary>
    ///     Determines whether two lists of raindrop items have different data.
    /// </summary>
    /// <param name="currentData">Items currently shown on the page.</param>
    /// <param name="newData">Fresh items from a refresh use case.</param>
    /// <returns>True when count, link, or title differs.</returns>
    public static bool HasDataChanged(IReadOnlyList<RaindropItem> currentData, IReadOnlyList<RaindropItem> newData)
    {
        if (currentData.Count != newData.Count) return true;

        for (var i = 0; i < currentData.Count; i++)
            if (!string.Equals(currentData[i].Link, newData[i].Link, StringComparison.Ordinal) ||
                !string.Equals(currentData[i].Title, newData[i].Title, StringComparison.Ordinal))
                return true;

        return false;
    }
}
