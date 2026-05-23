using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using static redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation.RaindropItemPresentationHelper;

namespace redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

public partial class Articles
{
    private const string CacheKey = "Articles";
    private readonly RaindropPageContext _context = new();

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

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
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
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

    protected string GetFallbackReason(RaindropItem article)
    {
        return ImagePlaceholderService.GetFallbackReason(article, _context.ImageUrlCache);
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

    private string GetImageUrl(RaindropItem article)
    {
        return ImagePlaceholderService.GetImageUrl(article, _context.ImageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string articleLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            articleLink,
            loadSuccess,
            _context.ImageUrlCache,
            Js,
            () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(article, _context.ImageUrlCache);
    }
}
