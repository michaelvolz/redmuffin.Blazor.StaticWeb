using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.VideosPage;

public partial class Videos
{
    private const string CacheKey = "Videos";
    private readonly RaindropPageContext _context = new();

    [Inject]
    private ILogger<Videos> Logger { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    [Inject]
    private IImageUrlResolver ImageUrlResolver { get; set; } = null!;

    [Inject]
    private IRaindropItemsCache RaindropItemsCache { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
#pragma warning disable MA0015 // Not method parameters — validating Blazor [Inject] properties
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(ImageUrlResolver);
        ArgumentNullException.ThrowIfNull(RaindropItemsCache);
#pragma warning restore MA0015

        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context,
            CacheKey,
            RaindropItemsCache,
            ct => RaindropAPI.GetVideosAsync(ct),
            () => ImageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            Logger).ConfigureAwait(false);

        StateHasChanged();

        _ = Task.Run(() => RaindropPageOrchestrator.RefreshInBackgroundAsync(
            _context, CacheKey, ct => RaindropAPI.GetVideosAsync(ct),
            RaindropItemsCache,
            () => ImageUrlResolver.PopulateImageUrlCacheAsync(
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
            () => ImageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            () => InvokeAsync(StateHasChanged),
            Logger);
    }
}
