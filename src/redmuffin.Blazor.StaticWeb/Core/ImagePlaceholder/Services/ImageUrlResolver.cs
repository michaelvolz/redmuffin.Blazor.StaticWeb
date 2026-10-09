using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Orchestrates image URL resolution for Raindrop items.
///     Populates an in-memory URL cache from each item's cover URL.
///     Load failure detection happens in the component through image
///     load events.
/// </summary>
internal sealed class ImageUrlResolver : IImageUrlResolver
{
    private readonly IImagePlaceholderService _imagePlaceholderService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ImageUrlResolver" /> class.
    /// </summary>
    /// <param name="imagePlaceholderService">The image placeholder service</param>
    public ImageUrlResolver(IImagePlaceholderService imagePlaceholderService)
    {
        _imagePlaceholderService =
            imagePlaceholderService
            ?? throw new ArgumentNullException(nameof(imagePlaceholderService));
    }

    /// <inheritdoc />
    public Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(imageUrlCache);
        ArgumentNullException.ThrowIfNull(stateHasChangedCallback);

        foreach (var item in items)
        {
            var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
            imageUrlCache[cacheKey] = string.IsNullOrEmpty(item.Cover)
                ? _imagePlaceholderService.GetDefaultPlaceholder()
                : item.Cover;
        }

        return Task.CompletedTask;
    }
}
