using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;

/// <summary>
///     Resolves what image URL to display for Raindrop items.
///     Orchestrates cache population, cached URL lookup, and background validation.
/// </summary>
public interface IImageUrlResolver
{
    /// <summary>
    ///     Populates the image URL cache for all items using ONLY cached values.
    ///     This method never triggers network requests, ensuring fast page loads.
    ///     Background validation is started for uncached images.
    /// </summary>
    Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the cached image URL for an item without triggering network requests.
    /// </summary>
    Task<string> GetCachedImageUrlAsync(RaindropItem item, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates an image in the background and updates the UI when complete.
    /// </summary>
    Task ValidateImageInBackgroundAsync(
        RaindropItem item,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default);
}