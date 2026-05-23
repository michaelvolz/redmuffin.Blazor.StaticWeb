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

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IImageUrlResolver ImageUrlResolver { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    [Inject]
    private IRaindropItemsCache RaindropItemsCache { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Validate injected dependencies
#pragma warning disable MA0015 // Not method parameters — validating Blazor [Inject] properties
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(ImageUrlResolver);
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        ArgumentNullException.ThrowIfNull(RaindropItemsCache);
#pragma warning restore MA0015

        // Load cached data first for immediate display
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context,
            CacheKey,
            RaindropItemsCache,
            ct => RaindropAPI.GetArticlesAsync(ct),
            PopulateImageUrlCacheAsync,
            Logger).ConfigureAwait(false);

        StateHasChanged();

        _ = Task.Run(() => RaindropPageOrchestrator.RefreshInBackgroundAsync(
            _context, CacheKey, ct => RaindropAPI.GetArticlesAsync(ct),
            RaindropItemsCache, PopulateImageUrlCacheAsync,
            () => InvokeAsync(StateHasChanged), Logger));
    }

    private Task HandleRefreshClickAsync()
    {
        return RaindropPageOrchestrator.HandleRefreshClickAsync(
            _context,
            CacheKey,
            ct => RaindropAPI.GetArticlesAsync(ct),
            RaindropItemsCache,
            PopulateImageUrlCacheAsync,
            () => InvokeAsync(StateHasChanged),
            Logger);
    }

    private Task PopulateImageUrlCacheAsync()
    {
        return ImageUrlResolver.PopulateImageUrlCacheAsync(
            _context.Items!,
            _context.ImageUrlCache,
            () => InvokeAsync(StateHasChanged),
            CancellationToken.None);
    }
}
