using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;

/// <summary>
///     Resolves what image URL to display for Raindrop items.
///     Populates the per-page image URL cache.
/// </summary>
public interface IImageUrlResolver
{
    /// <summary>
    ///     Populates the image URL cache from each item's cover URL.
    ///     This method never triggers network requests, ensuring fast page loads.
    ///     Image load failures surface through image load events in the component.
    /// </summary>
    Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default
    );
}
