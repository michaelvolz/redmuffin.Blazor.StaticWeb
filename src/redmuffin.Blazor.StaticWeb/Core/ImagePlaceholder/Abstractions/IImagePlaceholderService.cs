using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;

/// <summary>
/// Service for managing image placeholders and image URL handling.
/// </summary>
public interface IImagePlaceholderService
{
    /// <summary>
    /// Gets the default placeholder SVG for items without images.
    /// </summary>
    /// <returns>Base64-encoded SVG placeholder</returns>
    string GetDefaultPlaceholder();

    /// <summary>
    /// Generates a simple SVG placeholder with the specified failure reason.
    /// </summary>
    /// <param name="reason">The reason for the placeholder</param>
    /// <returns>Base64-encoded SVG placeholder with reason text</returns>
    string GenerateSimplePlaceholder(string reason);

    /// <summary>
    /// Gets the image URL for an item from the cache.
    /// This method is used by the UI for rendering.
    /// </summary>
    /// <param name="item">The item to get the image URL for</param>
    /// <param name="imageUrlCache">The image URL cache dictionary</param>
    /// <returns>The cached image URL or a default placeholder</returns>
    string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache);

    /// <summary>
    /// Handles image load events (success or failure) and updates the cache accordingly.
    /// </summary>
    /// <param name="elementId">The DOM element ID for shimmer control</param>
    /// <param name="itemLink">The item link used as cache key</param>
    /// <param name="loadSuccess">Whether the image loaded successfully</param>
    /// <param name="imageUrlCache">The image URL cache dictionary</param>
    /// <param name="jsRuntime">JavaScript runtime for DOM manipulation</param>
    /// <param name="stateHasChangedCallback">Callback to trigger UI updates</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleImageLoadAsync(
        string elementId,
        string itemLink,
        bool loadSuccess,
        IDictionary<string, string> imageUrlCache,
        IJSRuntime jsRuntime,
        Func<Task> stateHasChangedCallback);

    /// <summary>
    /// Determines if an item has a fallback placeholder.
    /// </summary>
    /// <param name="item">The item to check</param>
    /// <param name="imageUrlCache">The image URL cache dictionary</param>
    /// <returns>True if the item has a fallback placeholder, false otherwise</returns>
    bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache);

    /// <summary>
    /// Gets the fallback reason for an item.
    /// </summary>
    /// <param name="item">The item to get the fallback reason for</param>
    /// <param name="imageUrlCache">The image URL cache dictionary</param>
    /// <returns>The fallback reason text</returns>
    string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache);
}