using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Tests.Support.Images;

/// <summary>
///     Image URL resolver fake that fills the cache from item cover URLs and counts populate calls.
/// </summary>
public sealed class ImageUrlResolver_Mock : IImageUrlResolver
{
    /// <summary>
    ///     Gets the number of populate calls.
    /// </summary>
    public int PopulateCallCount { get; private set; }

    /// <inheritdoc />
    public Task PopulateImageUrlCacheAsync(
        IEnumerable<RaindropItem> items,
        IDictionary<string, string> imageUrlCache,
        Func<Task> stateHasChangedCallback,
        CancellationToken cancellationToken = default
    )
    {
        PopulateCallCount++;

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Cover))
            {
                var key = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
                imageUrlCache[key] = item.Cover;
            }
        }

        return stateHasChangedCallback();
    }
}
