using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Orchestrates image URL resolution for Raindrop items.
///     Popsulates an in-memory URL cache, resolves cached URLs, and runs
///     background HTTP validation. Delegates all HTTP operations to
///     <see cref="IImageValidator" /> and placeholder generation to
///     <see cref="IImagePlaceholderService" />.
/// </summary>
public sealed partial class ImageUrlResolver : IImageUrlResolver
{
    private readonly IImageValidator _imageValidator;
    private readonly IImagePlaceholderService _imagePlaceholderService;
    private readonly ILogger<ImageUrlResolver> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImageUrlResolver" /> class.
    /// </summary>
    /// <param name="imageValidator">The image validator for HTTP validation</param>
    /// <param name="imagePlaceholderService">The image placeholder service</param>
    /// <param name="logger">The logger instance</param>
    public ImageUrlResolver(
        IImageValidator imageValidator,
        IImagePlaceholderService imagePlaceholderService,
        ILogger<ImageUrlResolver> logger)
    {
        _imageValidator = imageValidator ?? throw new ArgumentNullException(nameof(imageValidator));
        _imagePlaceholderService = imagePlaceholderService ?? throw new ArgumentNullException(nameof(imagePlaceholderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(imageUrlCache);
        ArgumentNullException.ThrowIfNull(stateHasChangedCallback);

        var backgroundValidationTasks = new List<Task>();

        foreach (var item in items)
        {
            // Use only cached values - no network requests during initial render
            var imageUrl = await GetCachedImageUrlAsync(item, cancellationToken).ConfigureAwait(false);
            var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
            imageUrlCache[cacheKey] = imageUrl;

            // If we don't have a cached valid result, start background validation
            if (string.IsNullOrEmpty(item.Cover) ||
                imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                // Fire-and-forget background validation
                var backgroundTask = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await ValidateImageInBackgroundAsync(
                                item,
                                imageUrlCache,
                                stateHasChangedCallback,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogBackgroundValidationFailed(_logger, cacheKey, ex);
                        }
                    },
                    cancellationToken);
                backgroundValidationTasks.Add(backgroundTask);
            }
        }

        // Don't wait for background tasks to complete - they'll update the UI when done
        if (backgroundValidationTasks.Count > 0) LogBackgroundTasksStarted(_logger, backgroundValidationTasks.Count);
    }

    /// <inheritdoc />
    public async Task<string> GetCachedImageUrlAsync(RaindropItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        // If no cover image, return placeholder immediately
        if (string.IsNullOrEmpty(item.Cover))
            return _imagePlaceholderService.GetDefaultPlaceholder();

        // Check cache ONLY - no network requests
        var cachedResult = await _imageValidator.GetCachedResultAsync(item.Cover, cancellationToken).ConfigureAwait(false);

        if (cachedResult != null)
            // Use cached validation result
            return cachedResult.IsValid
                ? item.Cover
                : _imagePlaceholderService.GenerateSimplePlaceholder(cachedResult.FailureReason ?? "Image not available");

        // No cached result - use original cover image for now, background validation will handle it
        return item.Cover;
    }

    /// <inheritdoc />
    public async Task ValidateImageInBackgroundAsync(
        RaindropItem item,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(imageUrlCache);
        ArgumentNullException.ThrowIfNull(stateHasChangedCallback);

        if (string.IsNullOrEmpty(item.Cover)) return;

        try
        {
            // Perform actual validation
            var result = await _imageValidator.ValidateImageAsync(item.Cover, cancellationToken).ConfigureAwait(false);

            // Get the appropriate URL based on validation result
            var imageUrl = result.IsValid
                ? item.Cover
                : _imagePlaceholderService.GenerateSimplePlaceholder(result.FailureReason ?? "Image not available");

            // Update cache only if the validation result is different from current cache
            var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
            var currentCachedUrl = imageUrlCache.TryGetValue(cacheKey, out var cachedUrl) ? cachedUrl : string.Empty;
            if (!string.Equals(currentCachedUrl, imageUrl, StringComparison.Ordinal))
            {
                imageUrlCache[cacheKey] = imageUrl;

                // Trigger UI update on the main thread
                await stateHasChangedCallback().ConfigureAwait(false);

                LogBackgroundValidationCompleted(_logger, cacheKey, result.IsValid);
            }
        }
        catch (Exception ex)
        {
            var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
            LogBackgroundValidationFailed(_logger, cacheKey, ex);

            // On error, ensure we have a placeholder
            var placeholder = _imagePlaceholderService.GenerateSimplePlaceholder("Validation error");
            imageUrlCache[cacheKey] = placeholder;

            // Trigger UI update on the main thread
            await stateHasChangedCallback().ConfigureAwait(false);
        }
    }
}
