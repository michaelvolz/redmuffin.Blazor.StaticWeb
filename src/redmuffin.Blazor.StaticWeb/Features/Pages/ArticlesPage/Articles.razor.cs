using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

public partial class Articles
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, string, Exception?> LogRawJsonResponse =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(LogRawJsonResponse)),
            "Raw JSON Response: {JsonResponse}");

    private static readonly Action<ILogger, string?, string?, string?, Exception> LogJsonDeserializationError =
        LoggerMessage.Define<string?, string?, string?>(LogLevel.Error, new EventId(2, nameof(LogJsonDeserializationError)),
            "JSON Deserialization Error. Path: {Path}, LineNumber: {LineNumber}, BytePositionInLine: {BytePositionInLine}");

    private static readonly Action<ILogger, string, Exception> LogShimmerError =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(LogShimmerError)),
            "Error stopping shimmer for element: {ElementId}");

    // Simple state management - only what we need
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.OrdinalIgnoreCase);
    private List<RaindropItem>? _articleItems;

    private string? _errorMessage;
    private bool _isLoading;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISimpleImageValidationService SimpleImageValidationService { get; set; } = null!;

    /// <summary>
    ///     Gets the default placeholder SVG for articles without images.
    /// </summary>
    /// <returns>Base64-encoded SVG placeholder</returns>
    private static string GetDefaultPlaceholder()
    {
        return
            "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KICA8cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjVmNWY1IiBzdHJva2U9IiNkZGQiIHN0cm9rZS13aWR0aD0iMiIvPgogIDx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBkb21pbmFudC1iYXNlbGluZT0ibWlkZGxlIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LWZhbWlseT0iQXJpYWwsIHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTYiIGZpbGw9IiM5OTkiPk5vIEltYWdlIEF2YWlsYWJsZTwvdGV4dD4KPC9zdmc+";
    }

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

    protected override async Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(Http);
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(SimpleImageValidationService);

        // Load articles automatically when the page starts
        await FetchArticlesAsync().ConfigureAwait(false);
    }

    protected string GetFallbackReason(RaindropItem article)
    {
        var imageUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);

        if (string.IsNullOrEmpty(imageUrl))
            return "No image available";

        if (imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase))
            return "Image failed to load";

        return "Image not available";
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
            var response = await Http.GetAsync("/api/RaindropListArticles").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                LogRawJsonResponse(Logger, json, null);

                await ProcessSuccessfulResponseAsync(json).ConfigureAwait(false);
            }
            else
            {
                _errorMessage = $"Error fetching articles: {response.StatusCode} - {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}";
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

    private async Task ProcessSuccessfulResponseAsync(string json)
    {
        try
        {
            // Use JsonTypeInfo for deserialization to avoid trimming issues
            _articleItems = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList);

            // Populate image URL cache for initial render
            if (_articleItems is { Count: > 0 })
            {
                await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                StateHasChanged();
            }
        }
        catch (JsonException jsonEx)
        {
            LogJsonDeserializationError(
                Logger,
                jsonEx.Path?.ToString(CultureInfo.InvariantCulture),
                jsonEx.LineNumber?.ToString(CultureInfo.InvariantCulture),
                jsonEx.BytePositionInLine?.ToString(CultureInfo.InvariantCulture),
                jsonEx);
            _errorMessage = "Error deserializing JSON: " + jsonEx.Message;
        }
    }

    /// <summary>
    ///     Populates the image URL cache for all articles using a cache-first approach.
    ///     This method provides immediate image URLs for rendering while queuing background validation.
    /// </summary>
    private async Task PopulateImageUrlCacheAsync()
    {
        if (_articleItems == null) return;

        var backgroundValidationTasks = new List<Task>();

        foreach (var article in _articleItems)
        {
            var imageUrl = await GetImageUrlAsync(article).ConfigureAwait(false);
            _imageUrlCache[article.Link] = imageUrl;

            // If we got a placeholder or the image wasn't cached, start background validation
            if (!string.IsNullOrEmpty(article.Cover) &&
                (imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(imageUrl, article.Cover, StringComparison.Ordinal)))
            {
                // Fire-and-forget background validation
                var backgroundTask = Task.Run(async () =>
                {
                    try
                    {
                        await ValidateImageInBackgroundAsync(article).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Background validation failed for article: {ArticleLink}", article.Link);
                    }
                });
                backgroundValidationTasks.Add(backgroundTask);
            }
        }

        // Don't wait for background tasks to complete - they'll update the UI when done
        if (backgroundValidationTasks.Count > 0) Logger.LogDebug("Started {TaskCount} background validation tasks", backgroundValidationTasks.Count);
    }

    /// <summary>
    ///     Gets the best available image URL for an article, checking cache first.
    ///     This method integrates with the SimpleImageValidationService for optimal performance.
    /// </summary>
    /// <param name="article">The article to get the image URL for</param>
    /// <returns>A valid image URL or a placeholder if the image is not available</returns>
    private async Task<string> GetImageUrlAsync(RaindropItem article)
    {
        // If no cover image, return placeholder immediately
        if (string.IsNullOrEmpty(article.Cover)) return await SimpleImageValidationService.GetImageUrlOrPlaceholderAsync(string.Empty).ConfigureAwait(false);

        // Use the service to get the best URL (cached or validated)
        var imageUrl = await SimpleImageValidationService.GetImageUrlOrPlaceholderAsync(article.Cover).ConfigureAwait(false);

        // If the service returned a different URL (placeholder), trigger UI update
        if (!string.Equals(imageUrl, article.Cover, StringComparison.Ordinal))
            // Schedule a UI update on the next tick to reflect the placeholder
            _ = Task.Run(async () =>
            {
                await Task.Delay(1).ConfigureAwait(false); // Small delay to avoid blocking
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            });

        return imageUrl;
    }

    /// <summary>
    ///     Gets the image URL for an article from the cache.
    ///     This method is used by the UI for rendering.
    /// </summary>
    /// <param name="article">The article to get the image URL for</param>
    /// <returns>The cached image URL or a default placeholder</returns>
    private string GetImageUrl(RaindropItem article)
    {
        return _imageUrlCache.GetValueOrDefault(article.Link, GetDefaultPlaceholder());
    }

    /// <summary>
    ///     Validates an image in the background and updates the UI when complete.
    ///     This method runs in a fire-and-forget manner for optimal performance.
    /// </summary>
    /// <param name="article">The article to validate</param>
    private async Task ValidateImageInBackgroundAsync(RaindropItem article)
    {
        if (string.IsNullOrEmpty(article.Cover)) return;

        try
        {
            // Perform actual validation
            var result = await SimpleImageValidationService.ValidateImageAsync(article.Cover).ConfigureAwait(false);

            // Get the appropriate URL based on validation result
            var imageUrl = result.IsValid
                ? article.Cover
                : await SimpleImageValidationService.GetImageUrlOrPlaceholderAsync(article.Cover).ConfigureAwait(false);

            // Update cache only if the validation result is different from current cache
            var currentCachedUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);
            if (!string.Equals(currentCachedUrl, imageUrl, StringComparison.Ordinal))
            {
                _imageUrlCache[article.Link] = imageUrl;

                // Trigger UI update on the main thread
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);

                Logger.LogDebug(
                    "Background validation completed for article: {ArticleLink}, Valid: {IsValid}",
                    article.Link,
                    result.IsValid);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Background validation failed for article: {ArticleLink}", article.Link);

            // On error, ensure we have a placeholder
            var placeholder = await SimpleImageValidationService.GetImageUrlOrPlaceholderAsync(string.Empty).ConfigureAwait(false);
            _imageUrlCache[article.Link] = placeholder;

            // Trigger UI update on the main thread
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task StopShimmerAsync(string elementId)
    {
        try
        {
            await Js.InvokeVoidAsync("eval", $"document.getElementById('{elementId}')?.classList.add('loaded')").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogShimmerError(Logger, elementId, ex);
        }
    }

    private async Task HandleImageLoadAsync(string elementId, string articleLink, bool loadSuccess)
    {
        try
        {
            if (!loadSuccess)
            {
                // Replace with placeholder if image failed to load
                var placeholder = await SimpleImageValidationService.GetImageUrlOrPlaceholderAsync(_imageUrlCache.GetValueOrDefault(articleLink, string.Empty))
                    .ConfigureAwait(false);
                _imageUrlCache[articleLink] = placeholder;
                StateHasChanged();
            }

            // Stop shimmer effect
            await StopShimmerAsync(elementId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error handling image load for article: {ArticleLink}", articleLink);
        }
    }

    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        var imageUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);
        return string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase);
    }
}