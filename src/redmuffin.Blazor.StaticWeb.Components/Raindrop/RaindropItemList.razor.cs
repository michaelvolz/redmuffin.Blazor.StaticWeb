using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Components.Raindrop;

/// <summary>
///     Renders a masonry grid of Raindrop items (articles or videos). Handles image
///     loading, fallback placeholders, and shimmer effects internally.
/// </summary>
public partial class RaindropItemList
{
    public const string EmptyStateElementId = "empty-state";

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
            id => StopShimmerAsync(id),
            () => InvokeAsync(StateHasChanged));
    }

    private async Task StopShimmerAsync(string elementId)
    {
        try
        {
            await Js.InvokeVoidAsync(
                "eval",
                $"document.getElementById('{elementId}')?.classList.add('loaded')").ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // Shimmer stop is best-effort; image state still updates.
            _ = ex;
        }
        catch (InvalidOperationException ex)
        {
            // Shimmer stop is best-effort; image state still updates.
            _ = ex;
        }
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
