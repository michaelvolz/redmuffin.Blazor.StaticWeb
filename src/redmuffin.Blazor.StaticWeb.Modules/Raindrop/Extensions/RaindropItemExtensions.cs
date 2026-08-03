using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Extensions;

/// <summary>
///     Extension methods for converting between RaindropItem and PrunedRaindropItem.
/// </summary>
public static class RaindropItemExtensions
{
    /// <summary>
    ///     Converts a RaindropItem to a PrunedRaindropItem containing only essential fields.
    /// </summary>
    /// <param name="item">The RaindropItem to convert.</param>
    /// <returns>A PrunedRaindropItem with essential fields copied from the source item.</returns>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    public static PrunedRaindropItem ToPruned(this RaindropItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new PrunedRaindropItem
        {
            Id = item.Id,
            Link = item.Link,
            Title = item.Title,
            Excerpt = item.Excerpt,
            Cover = item.Cover
        };
    }

    /// <summary>
    ///     Converts a collection of RaindropItems to PrunedRaindropItems.
    /// </summary>
    /// <param name="items">The collection of RaindropItems to convert.</param>
    /// <returns>A collection of PrunedRaindropItems.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
    public static IEnumerable<PrunedRaindropItem> ToPruned(this IEnumerable<RaindropItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.Select(item => item.ToPruned());
    }

    /// <summary>
    ///     Converts a PrunedRaindropItem back to a RaindropItem with default values for non-essential fields.
    /// </summary>
    /// <param name="prunedItem">The PrunedRaindropItem to unprune.</param>
    /// <returns>A RaindropItem with essential fields copied from the pruned item.</returns>
    /// <exception cref="ArgumentNullException">Thrown when prunedItem is null.</exception>
    public static RaindropItem ToUnpruned(this PrunedRaindropItem prunedItem)
    {
        ArgumentNullException.ThrowIfNull(prunedItem);

        return new RaindropItem
        {
            Id = prunedItem.Id,
            Link = prunedItem.Link,
            Title = prunedItem.Title,
            Excerpt = prunedItem.Excerpt,
            Cover = prunedItem.Cover,
            Note = null,
            Type = null,
            Domain = null
        };
    }

    /// <summary>
    ///     Converts a collection of PrunedRaindropItems back to RaindropItems.
    /// </summary>
    /// <param name="prunedItems">The collection of PrunedRaindropItems to convert.</param>
    /// <returns>A collection of RaindropItems.</returns>
    /// <exception cref="ArgumentNullException">Thrown when prunedItems is null.</exception>
    public static IEnumerable<RaindropItem> ToUnpruned(this IEnumerable<PrunedRaindropItem> prunedItems)
    {
        ArgumentNullException.ThrowIfNull(prunedItems);

        return prunedItems.Select(item => item.ToUnpruned());
    }
}