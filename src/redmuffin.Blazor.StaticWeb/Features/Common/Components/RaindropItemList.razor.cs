using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

/// <summary>
///     Renders a masonry grid of Raindrop items (articles or videos). Handles image
///     loading, fallback placeholders, and shimmer effects internally.
/// </summary>
public partial class RaindropItemList
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<RaindropItem>? Items { get; set; }

    [Parameter]
    [EditorRequired]
    public IDictionary<string, string> ImageUrlCache { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string CardCssClass { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string ImageAlt { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string LinkButtonText { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string LinkButtonIcon { get; set; } = string.Empty;

    [Parameter]
    public string EmptyMessage { get; set; } = "No items available.";

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private string GetImageUrl(RaindropItem item)
    {
        return ImagePlaceholderService.GetImageUrl(item, ImageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string itemLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            itemLink,
            loadSuccess,
            ImageUrlCache,
            Js,
            () => InvokeAsync(StateHasChanged));
    }

    private bool HasFallbackPlaceholder(RaindropItem item)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(item, ImageUrlCache);
    }

    private string GetFallbackReason(RaindropItem item)
    {
        return ImagePlaceholderService.GetFallbackReason(item, ImageUrlCache);
    }
}
