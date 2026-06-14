using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

public partial class Articles
{
    private const string CacheKey = "Articles";
    private readonly RaindropPageContext _context = new();

    private readonly ILogger<Articles> _logger;
    private readonly NavigationManager _navigation;
    private readonly IImageUrlResolver _imageUrlResolver;
    private readonly IRaindropAPI _raindropAPI;
    private readonly IRaindropItemsCache _raindropItemsCache;

    public Articles(
        ILogger<Articles> logger,
        NavigationManager navigation,
        IImageUrlResolver imageUrlResolver,
        IRaindropAPI raindropAPI,
        IRaindropItemsCache raindropItemsCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _imageUrlResolver = imageUrlResolver ?? throw new ArgumentNullException(nameof(imageUrlResolver));
        _raindropAPI = raindropAPI ?? throw new ArgumentNullException(nameof(raindropAPI));
        _raindropItemsCache = raindropItemsCache ?? throw new ArgumentNullException(nameof(raindropItemsCache));
    }

    protected override async Task OnInitializedAsync()
    {
        // Load cached data first for immediate display
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context,
            CacheKey,
            _raindropItemsCache,
            ct => _raindropAPI.GetArticlesAsync(ct),
            PopulateImageUrlCacheAsync,
            _logger).ConfigureAwait(false);

        StateHasChanged();

        _ = Task.Run(() => RaindropPageOrchestrator.RefreshInBackgroundAsync(
            _context, CacheKey, ct => _raindropAPI.GetArticlesAsync(ct),
            _raindropItemsCache, PopulateImageUrlCacheAsync,
            () => InvokeAsync(StateHasChanged), _logger));
    }

    private Task HandleRefreshClickAsync()
    {
        return RaindropPageOrchestrator.HandleRefreshClickAsync(
            _context,
            CacheKey,
            ct => _raindropAPI.GetArticlesAsync(ct),
            _raindropItemsCache,
            PopulateImageUrlCacheAsync,
            () => InvokeAsync(StateHasChanged),
            _logger);
    }

    private Task PopulateImageUrlCacheAsync()
    {
        return _imageUrlResolver.PopulateImageUrlCacheAsync(
            _context.Items!,
            _context.ImageUrlCache,
            () => InvokeAsync(StateHasChanged),
            CancellationToken.None);
    }
}
