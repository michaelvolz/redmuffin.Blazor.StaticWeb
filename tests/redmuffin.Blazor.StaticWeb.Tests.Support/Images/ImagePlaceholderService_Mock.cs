using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Tests.Support.Images;

/// <summary>
///     Image placeholder service fake with configurable results and recorded calls.
/// </summary>
public sealed class ImagePlaceholderService_Mock : IImagePlaceholderService
{
    private const string DefaultPlaceholder =
        "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCI+PC9zdmc+";

    private readonly List<ImageLoadCall> _imageLoadCalls = [];
    private readonly List<(string? ItemLink, bool HasFallback)> _fallbackPlaceholderCalls = [];
    private readonly Dictionary<string, string> _imageUrls = new Dictionary<string, string>(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, bool> _fallbackStatuses = new Dictionary<string, bool>(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, string> _fallbackReasons = new Dictionary<string, string>(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, string> _simplePlaceholders = new Dictionary<
        string,
        string
    >(StringComparer.Ordinal);
    private string _defaultPlaceholder = DefaultPlaceholder;

    /// <summary>
    ///     Gets the recorded image load calls.
    /// </summary>
    public IReadOnlyList<ImageLoadCall> ImageLoadCalls => _imageLoadCalls;

    /// <summary>
    ///     Gets the recorded image load calls under the name used by the Articles page tests.
    /// </summary>
    public IReadOnlyList<ImageLoadCall> HandleImageLoadCalls => _imageLoadCalls;

    /// <summary>
    ///     Gets the recorded fallback placeholder checks.
    /// </summary>
    public IReadOnlyList<(string? ItemLink, bool HasFallback)> FallbackPlaceholderCalls =>
        _fallbackPlaceholderCalls;

    /// <summary>
    ///     Sets the placeholder returned when no image URL is known.
    /// </summary>
    /// <param name="placeholder">Placeholder value to return.</param>
    public void SetupDefaultPlaceholder(string placeholder)
    {
        _defaultPlaceholder = placeholder;
    }

    /// <summary>
    ///     Sets the placeholder returned for a failure reason.
    /// </summary>
    /// <param name="reason">Failure reason key.</param>
    /// <param name="result">Placeholder value to return.</param>
    public void SetupSimplePlaceholder(string reason, string result)
    {
        _simplePlaceholders[reason] = result;
    }

    /// <summary>
    ///     Sets the image URL returned for an item link.
    /// </summary>
    /// <param name="itemLink">Item link key; a null link uses the "null-link" key.</param>
    /// <param name="resultUrl">Image URL to return.</param>
    public void SetupImageUrl(string? itemLink, string resultUrl)
    {
        _imageUrls[itemLink ?? "null-link"] = resultUrl;
    }

    /// <summary>
    ///     Sets the fallback status reported for an item link.
    /// </summary>
    /// <param name="itemLink">Item link key; a null link uses the "null-link" key.</param>
    /// <param name="hasFallback">Fallback status to report.</param>
    public void SetupFallbackStatus(string? itemLink, bool hasFallback)
    {
        _fallbackStatuses[itemLink ?? "null-link"] = hasFallback;
    }

    /// <summary>
    ///     Sets the fallback reason returned for an item link.
    /// </summary>
    /// <param name="itemLink">Item link key; a null link uses the "null-link" key.</param>
    /// <param name="reason">Fallback reason to return.</param>
    public void SetupFallbackReason(string? itemLink, string reason)
    {
        _fallbackReasons[itemLink ?? "null-link"] = reason;
    }

    /// <summary>
    ///     Clears every configured value and restores the default placeholder.
    /// </summary>
    public void Reset()
    {
        _imageUrls.Clear();
        _fallbackStatuses.Clear();
        _fallbackReasons.Clear();
        _simplePlaceholders.Clear();
        _defaultPlaceholder = DefaultPlaceholder;
    }

    /// <inheritdoc />
    public string GetDefaultPlaceholder()
    {
        return _defaultPlaceholder;
    }

    /// <inheritdoc />
    public string GenerateSimplePlaceholder(string reason)
    {
        return _simplePlaceholders.TryGetValue(reason, out var placeholder)
            ? placeholder
            : $"data:image/svg+xml;base64,placeholder-{reason}";
    }

    /// <inheritdoc />
    public string GetImageUrl(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        var key = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
        return _imageUrls.TryGetValue(key, out var url) ? url
            : imageUrlCache.TryGetValue(key, out var cachedUrl) ? cachedUrl
            : _defaultPlaceholder;
    }

    /// <inheritdoc />
    public async Task HandleImageLoadAsync(
        string elementId,
        string itemLink,
        bool loadSuccess,
        IDictionary<string, string> imageUrlCache,
        Func<string, Task> stopShimmerAsync,
        Func<Task> stateHasChangedCallback
    )
    {
        _imageLoadCalls.Add(new ImageLoadCall(elementId, itemLink, loadSuccess, imageUrlCache));
        await stopShimmerAsync(elementId).ConfigureAwait(false);
        await stateHasChangedCallback().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool HasFallbackPlaceholder(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        var key = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
        var hasFallback = _fallbackStatuses.TryGetValue(key, out var status) && status;
        _fallbackPlaceholderCalls.Add((key, hasFallback));
        return hasFallback;
    }

    /// <inheritdoc />
    public string GetFallbackReason(RaindropItem item, IDictionary<string, string> imageUrlCache)
    {
        var key = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
        return _fallbackReasons.TryGetValue(key, out var reason) ? reason : string.Empty;
    }
}
