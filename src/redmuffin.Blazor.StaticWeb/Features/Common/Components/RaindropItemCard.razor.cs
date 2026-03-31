using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class RaindropItemCard
{
    [Parameter]
    public RaindropItem Item { get; set; } = null!;

    [Parameter]
    public string ItemType { get; set; } = null!;

    [Parameter]
    public int Index { get; set; }

    [Parameter]
    public IDictionary<string, string> ImageUrlCache { get; set; } = null!;

    [Parameter]
    public IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Parameter]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter]
    public EventCallback<(string ElementId, string ItemLink, bool Success)> OnImageLoad { get; set; }

    private bool IsEagerLoad => Index < 6;

    private string GetCardClass() => ItemType.ToLowerInvariant() switch
    {
        "video" => "video-card",
        "article" => "article-card",
        _ => string.Empty
    };

    private string GetImageUrl() => ImagePlaceholderService.GetImageUrl(Item, ImageUrlCache);

    private string GetImageAlt() => ItemType.ToLowerInvariant() switch
    {
        "video" => "Video Cover",
        "article" => "Article Cover",
        _ => "Cover Image"
    };

    private string GetButtonIcon() => ItemType.ToLowerInvariant() switch
    {
        "video" => "fa-play",
        "article" => "fa-external-link-alt",
        _ => "fa-link"
    };

    private string GetButtonText() => ItemType.ToLowerInvariant() switch
    {
        "video" => "Watch Video",
        "article" => "Read Article",
        _ => "View"
    };

    private bool HasFallbackPlaceholder() => ImagePlaceholderService.HasFallbackPlaceholder(Item, ImageUrlCache);

    private string GetFallbackReason() => ImagePlaceholderService.GetFallbackReason(Item, ImageUrlCache);

    private static string DisplayTitle(RaindropItem item)
    {
        return string.IsNullOrEmpty(item.Title) ? "No Title Available" : item.Title;
    }

    private static string DisplayExcerpt(RaindropItem item)
    {
        return string.IsNullOrEmpty(item.Excerpt)
            ? "No Excerpt Available"
            : item.Excerpt.Length > 250
                ? string.Concat(item.Excerpt.AsSpan(0, 250), "...")
                : item.Excerpt;
    }

    private Task HandleImageLoadAsync(bool success)
    {
        var elementId = FormattableString.Invariant($"shimmer-{Item.Id}");
        return OnImageLoad.InvokeAsync((elementId, Item.Link, success));
    }
}
