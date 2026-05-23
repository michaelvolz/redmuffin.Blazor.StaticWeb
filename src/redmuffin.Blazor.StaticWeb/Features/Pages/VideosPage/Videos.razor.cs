using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using static redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation.RaindropItemPresentationHelper;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    private const string CacheKey = "Videos";
    private readonly RaindropPageContext _context = new();

    [Inject]
    private ILogger<Videos> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IImageValidationCacheService ImageValidationCacheService { get; set; } = null!;

    [Inject]
    private IRaindropItemsCache RaindropItemsCache { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
#pragma warning disable MA0015 // Not method parameters — validating Blazor [Inject] properties
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
        ArgumentNullException.ThrowIfNull(ImageValidationCacheService);
        ArgumentNullException.ThrowIfNull(RaindropItemsCache);
#pragma warning restore MA0015

        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context,
            CacheKey,
            RaindropItemsCache,
            ct => RaindropAPI.GetVideosAsync(ct),
            () => ImageValidationCacheService.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            Logger).ConfigureAwait(false);

        StateHasChanged();

        _ = Task.Run(() => RaindropPageOrchestrator.RefreshInBackgroundAsync(
            _context, CacheKey, ct => RaindropAPI.GetVideosAsync(ct),
            RaindropItemsCache,
            () => ImageValidationCacheService.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            () => InvokeAsync(StateHasChanged), Logger));
    }

    private Task HandleRefreshClickAsync()
    {
        return RaindropPageOrchestrator.HandleRefreshClickAsync(
            _context,
            CacheKey,
            ct => RaindropAPI.GetVideosAsync(ct),
            RaindropItemsCache,
            () => ImageValidationCacheService.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            () => InvokeAsync(StateHasChanged),
            Logger);
    }

    private string GetDefaultPlaceholder()
    {
        return ImagePlaceholderService.GetDefaultPlaceholder();
    }

    private string GetImageUrl(RaindropItem video)
    {
        return ImagePlaceholderService.GetImageUrl(video, _context.ImageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string videoLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            videoLink,
            loadSuccess,
            _context.ImageUrlCache,
            Js,
            () => InvokeAsync(StateHasChanged));
    }

    private bool HasFallbackPlaceholder(RaindropItem video)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(video, _context.ImageUrlCache);
    }

    private string GetFallbackReason(RaindropItem video)
    {
        return ImagePlaceholderService.GetFallbackReason(video, _context.ImageUrlCache);
    }
}
