using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

public partial class Articles
{
    // Simple state management - only what we need
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.OrdinalIgnoreCase);
    private List<RaindropItem>? _articleItems;

    private string? _errorMessage;
    private bool _isLoading;

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IImageValidationCacheService ImageValidationCacheService { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    private static string DisplayTitle(RaindropItem article)
    {
        return string.IsNullOrEmpty(article.Title) ? "No Title Available" : article.Title;
    }

    private static string DisplayExcerpt(RaindropItem article)
    {
        return string.IsNullOrEmpty(article.Excerpt)
            ? "No Excerpt Available"
            : article.Excerpt.Length > 250
                ? string.Concat(article.Excerpt.AsSpan(0, 250), "...")
                : article.Excerpt;
    }

    protected override Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
        ArgumentNullException.ThrowIfNull(ImageValidationCacheService);
        ArgumentNullException.ThrowIfNull(RaindropAPI);

        // Load articles automatically when the page starts
        return FetchArticlesAsync();
    }

    protected string GetFallbackReason(RaindropItem article)
    {
        return ImagePlaceholderService.GetFallbackReason(article, _imageUrlCache);
    }

    private async Task FetchArticlesAsync()
    {
        _errorMessage = null;
        _articleItems = null;
        _isLoading = true;

        // Clear image cache when fetching new articles
        _imageUrlCache.Clear();

        StateHasChanged();

        try
        {
            var articles = await RaindropAPI.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);
            _articleItems = articles.ToList();

            // Populate image URL cache for initial render
            if (_articleItems is { Count: > 0 })
            {
                await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Exception fetching articles: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }

        StateHasChanged();
    }

    /// <summary>
    ///     Populates the image URL cache for all articles using ONLY cached values.
    ///     This method never triggers network requests, ensuring fast page loads.
    ///     Background validation is started for uncached images.
    /// </summary>
    private async Task PopulateImageUrlCacheAsync()
    {
        if (_articleItems == null) return;

        await ImageValidationCacheService.PopulateImageUrlCacheAsync(
            _articleItems,
            _imageUrlCache,
            () => InvokeAsync(StateHasChanged),
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the image URL for an article from the cache.
    ///     This method is used by the UI for rendering.
    /// </summary>
    /// <param name="article">The article to get the image URL for</param>
    /// <returns>The cached image URL or a default placeholder</returns>
    private string GetImageUrl(RaindropItem article)
    {
        return ImagePlaceholderService.GetImageUrl(article, _imageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string articleLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            articleLink,
            loadSuccess,
            _imageUrlCache,
            Js,
            () =>
            {
                StateHasChanged();
                return Task.CompletedTask;
            });
    }

    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(article, _imageUrlCache);
    }
}