using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;

/// <summary>
/// Service for managing image validation caching and background validation.
/// </summary>
public interface IImageValidationCacheService
{
    /// <summary>
    /// Populates the image URL cache for all items using ONLY cached values.
    /// This method never triggers network requests, ensuring fast page loads.
    /// Background validation is started for uncached images.
    /// </summary>
    /// <param name="items">The items to populate cache for</param>
    /// <param name="imageUrlCache">The image URL cache dictionary to populate</param>
    /// <param name="stateHasChangedCallback">Callback to trigger UI updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the cached image URL for an item without triggering network requests.
    /// Returns the original cover image if cached as valid, or a placeholder if cached as invalid or not cached.
    /// </summary>
    /// <param name="item">The item to get the image URL for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A cached image URL or placeholder</returns>
    Task<string> GetCachedImageUrlAsync(RaindropItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an image in the background and updates the UI when complete.
    /// This method runs in a fire-and-forget manner for optimal performance.
    /// </summary>
    /// <param name="item">The item to validate</param>
    /// <param name="imageUrlCache">The image URL cache dictionary</param>
    /// <param name="stateHasChangedCallback">Callback to trigger UI updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ValidateImageInBackgroundAsync(
        RaindropItem item,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default);
}