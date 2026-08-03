using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;

/// <summary>
///     Service for managing image placeholders and image URL handling.
/// </summary>
public interface IImagePlaceholderService
{
    /// <summary>
    ///     Gets the default placeholder SVG for items without images.
    /// </summary>
    string GetDefaultPlaceholder();

    /// <summary>
    ///     Generates a simple SVG placeholder with the specified failure reason.
    /// </summary>
    string GenerateSimplePlaceholder(string reason);

    /// <summary>
    ///     Gets the image URL for an item from the cache.
    /// </summary>
    string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache);

    /// <summary>
    ///     Handles image load events (success or failure) and updates the cache accordingly.
    ///     <paramref name="stopShimmerAsync"/> stops the loading shimmer for the element
    ///     (host supplies JS interop; modules stay free of IJSRuntime).
    /// </summary>
    Task HandleImageLoadAsync(
        string elementId,
        string itemLink,
        bool loadSuccess,
        IDictionary<string, string> imageUrlCache,
        Func<string, Task> stopShimmerAsync,
        Func<Task> stateHasChangedCallback);

    /// <summary>
    ///     Determines if an item has a fallback placeholder.
    /// </summary>
    bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache);

    /// <summary>
    ///     Gets the fallback reason for an item.
    /// </summary>
    string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache);
}