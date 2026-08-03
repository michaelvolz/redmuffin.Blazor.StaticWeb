using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.VideosPage;

public partial class Videos
{
    private const string CacheKey = "Videos";
    private readonly RaindropPageContext _context = new();

    private readonly ILogger<Videos> _logger;
    private readonly NavigationManager _navigation;
    private readonly IRaindropAPI _raindropAPI;
    private readonly IImageUrlResolver _imageUrlResolver;
    private readonly IRaindropItemsCache _raindropItemsCache;

    public Videos(
        ILogger<Videos> logger,
        NavigationManager navigation,
        IRaindropAPI raindropAPI,
        IImageUrlResolver imageUrlResolver,
        IRaindropItemsCache raindropItemsCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _raindropAPI = raindropAPI ?? throw new ArgumentNullException(nameof(raindropAPI));
        _imageUrlResolver = imageUrlResolver ?? throw new ArgumentNullException(nameof(imageUrlResolver));
        _raindropItemsCache = raindropItemsCache ?? throw new ArgumentNullException(nameof(raindropItemsCache));
    }

    /// <summary>
    ///     Exposes the background refresh task so callers (including tests) can
    ///     await initialization completion deterministically without polling or delays.
    /// </summary>
    public Task? BackgroundRefreshTask { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context,
            CacheKey,
            _raindropItemsCache,
            ct => _raindropAPI.GetVideosAsync(ct),
            () => _imageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            _logger).ConfigureAwait(false);

        StateHasChanged();

        BackgroundRefreshTask = Task.Run(() => RaindropPageOrchestrator.RefreshInBackgroundAsync(
            _context, CacheKey, ct => _raindropAPI.GetVideosAsync(ct),
            _raindropItemsCache,
            () => _imageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            () => InvokeAsync(StateHasChanged), _logger));
    }

    private Task HandleRefreshClickAsync()
    {
        return RaindropPageOrchestrator.HandleRefreshClickAsync(
            _context,
            CacheKey,
            ct => _raindropAPI.GetVideosAsync(ct),
            _raindropItemsCache,
            () => _imageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items!, _context.ImageUrlCache, () => InvokeAsync(StateHasChanged), CancellationToken.None),
            () => InvokeAsync(StateHasChanged),
            _logger);
    }
}
