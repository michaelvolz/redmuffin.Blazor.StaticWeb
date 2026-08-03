using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Templates;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Service for managing image placeholders and image URL handling.
/// </summary>
public sealed partial class ImagePlaceholderService : IImagePlaceholderService
{
    private readonly ILogger<ImagePlaceholderService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImagePlaceholderService" /> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public ImagePlaceholderService(ILogger<ImagePlaceholderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GetDefaultPlaceholder()
    {
        return SvgPlaceholderTemplate.GenerateDefault();
    }

    /// <inheritdoc />
    public string GenerateSimplePlaceholder(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        var displayReason = SvgPlaceholderTemplate.MapFailureReasonToDisplayText(reason);
        return SvgPlaceholderTemplate.Generate(displayReason);
    }

    /// <inheritdoc />
    public string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(imageUrlCache);

        var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);

        // Check if there's a cached result
        if (imageUrlCache.TryGetValue(cacheKey, out var cachedResult))
        {
            // If cached as failed, return placeholder
            if (string.Equals(cachedResult, "FAILED", StringComparison.Ordinal)) return GetDefaultPlaceholder();

            // Return the cached URL
            return cachedResult;
        }

        // No cache entry - return the item's cover URL if available, otherwise placeholder
        return string.IsNullOrEmpty(item.Cover) ? GetDefaultPlaceholder() : item.Cover;
    }

    /// <inheritdoc />
    public async Task HandleImageLoadAsync(
        string elementId,
        string itemLink,
        bool loadSuccess,
        IDictionary<string, string> imageUrlCache,
        Func<string, Task> stopShimmerAsync,
        Func<Task> stateHasChangedCallback)
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(itemLink);
        ArgumentNullException.ThrowIfNull(imageUrlCache);
        ArgumentNullException.ThrowIfNull(stopShimmerAsync);
        ArgumentNullException.ThrowIfNull(stateHasChangedCallback);

        try
        {
            if (!loadSuccess) imageUrlCache[itemLink] = "FAILED";

            await stopShimmerAsync(elementId).ConfigureAwait(false);
            await stateHasChangedCallback().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogImageLoadHandlingError(_logger, elementId, itemLink, ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(imageUrlCache);

        // Has fallback if no cover URL or if cached as failed
        if (string.IsNullOrEmpty(item.Cover)) return true;

        var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
        if (imageUrlCache.TryGetValue(cacheKey, out var cachedResult) && string.Equals(cachedResult, "FAILED", StringComparison.Ordinal)) return true;

        return false;
    }

    /// <inheritdoc />
    public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(imageUrlCache);

        if (string.IsNullOrEmpty(item.Cover)) return SvgPlaceholderTemplate.MapFailureReasonToDisplayText("NO_IMAGE");

        var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
        if (imageUrlCache.TryGetValue(cacheKey, out var cachedResult) && string.Equals(cachedResult, "FAILED", StringComparison.Ordinal))
            return SvgPlaceholderTemplate.MapFailureReasonToDisplayText("LOAD_FAILED");

        return string.Empty;
    }
}