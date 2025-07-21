using System.Globalization;
using System.Text;
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

    private static readonly Action<ILogger, string, Exception?> LogBackgroundValidationFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, nameof(LogBackgroundValidationFailed)),
            "Background validation failed for article: {ArticleLink}");

    private static readonly Action<ILogger, int, Exception?> LogBackgroundTasksStarted =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(5, nameof(LogBackgroundTasksStarted)),
            "Started {TaskCount} background validation tasks");

    private static readonly Action<ILogger, string, bool, Exception?> LogBackgroundValidationCompleted =
        LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(6, nameof(LogBackgroundValidationCompleted)),
            "Background validation completed for article: {ArticleLink}, Valid: {IsValid}");

    private static readonly Action<ILogger, string, Exception> LogImageLoadHandlingError =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(7, nameof(LogImageLoadHandlingError)),
            "Error handling image load for article: {ArticleLink}");

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
            "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KICA8cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjVmNWY1IiBzdHJva2U9IiNkZGQiIHN0cm9rZS13aWR0aD0iMiIvPgogIDx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBkb21pbmFudC1iYXNlbGluZT0ibWlkZGxlIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LWZhbWlseT0iQXJpYWwsIHNhbnMtc2VyaWYiIGZvcnQtc2l6ZT0iMTYiIGZpbGw9IiM5OTkiPk5vIEltYWdlIEF2YWlsYWJsZTwvdGV4dD4KPC9zdmc+";
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

    /// <summary>
    ///     Generates a simple SVG placeholder with the failure reason.
    /// </summary>
    private static string GenerateSimplePlaceholder(string reason)
    {
        // Standard failure reasons mapping
        var displayReason = reason switch
        {
            var r when r.Contains("CORS", StringComparison.OrdinalIgnoreCase) => "CORS blocked",
            var r when r.Contains("404", StringComparison.OrdinalIgnoreCase) => "Image not found",
            var r when r.Contains("timeout", StringComparison.OrdinalIgnoreCase) => "Network error",
            var r when r.Contains("content type", StringComparison.OrdinalIgnoreCase) => "Invalid format",
            _ => "Image not available"
        };

        var svg = $@"<svg width=""400"" height=""200"" xmlns=""http://www.w3.org/2000/svg"">
  <rect width=""100%"" height=""100%"" fill=""#f5f5f5"" stroke=""#ddd"" stroke-width=""2""/>
  <text x=""50%"" y=""50%"" dominant-baseline=""middle"" text-anchor=""middle"" font-family=""Arial, sans-serif"" font-size=""16"" fill=""#999"">{displayReason}</text>
</svg>";

        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    protected override Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(Http);
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(SimpleImageValidationService);

        // Load articles automatically when the page starts
        return FetchArticlesAsync();
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
    ///     Populates the image URL cache for all articles using ONLY cached values.
    ///     This method never triggers network requests, ensuring fast page loads.
    ///     Background validation is started for uncached images.
    /// </summary>
    private async Task PopulateImageUrlCacheAsync()
    {
        if (_articleItems == null) return;

        var backgroundValidationTasks = new List<Task>();

        foreach (var article in _articleItems)
        {
            // Use only cached values - no network requests during initial render
            var imageUrl = await GetCachedImageUrlAsync(article).ConfigureAwait(false);
            _imageUrlCache[article.Link] = imageUrl;

            // If we don't have a cached valid result, start background validation
            if (string.IsNullOrEmpty(article.Cover) ||
                imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase))
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
                        LogBackgroundValidationFailed(Logger, article.Link, ex);
                    }
                });
                backgroundValidationTasks.Add(backgroundTask);
            }
        }

        // Don't wait for background tasks to complete - they'll update the UI when done
        if (backgroundValidationTasks.Count > 0) LogBackgroundTasksStarted(Logger, backgroundValidationTasks.Count, null);
    }

    /// <summary>
    ///     Gets the cached image URL for an article without triggering network requests.
    ///     Returns the original cover image if cached as valid, or a placeholder if cached as invalid or not cached.
    /// </summary>
    /// <param name="article">The article to get the image URL for</param>
    /// <returns>A cached image URL or placeholder</returns>
    private async Task<string> GetCachedImageUrlAsync(RaindropItem article)
    {
        // If no cover image, return placeholder immediately
        if (string.IsNullOrEmpty(article.Cover))
            return GetDefaultPlaceholder();

        // Check cache ONLY - no network requests
        var cachedResult = await SimpleImageValidationService.GetCachedResultAsync(article.Cover).ConfigureAwait(false);

        if (cachedResult != null)
            // Use cached validation result
            return cachedResult.IsValid ? article.Cover : GenerateSimplePlaceholder(cachedResult.FailureReason ?? "Image not available");

        // No cached result - use original cover image for now, background validation will handle it
        return article.Cover;
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
                : GenerateSimplePlaceholder(result.FailureReason ?? "Image not available");

            // Update cache only if the validation result is different from current cache
            var currentCachedUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);
            if (!string.Equals(currentCachedUrl, imageUrl, StringComparison.Ordinal))
            {
                _imageUrlCache[article.Link] = imageUrl;

                // Trigger UI update on the main thread
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);

                LogBackgroundValidationCompleted(Logger, article.Link, result.IsValid, null);
            }
        }
        catch (Exception ex)
        {
            LogBackgroundValidationFailed(Logger, article.Link, ex);

            // On error, ensure we have a placeholder
            var placeholder = GenerateSimplePlaceholder("Validation error");
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
                var placeholder = GenerateSimplePlaceholder("Image load failed");
                _imageUrlCache[articleLink] = placeholder;
                StateHasChanged();
            }

            // Stop shimmer effect
            await StopShimmerAsync(elementId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogImageLoadHandlingError(Logger, articleLink, ex);
        }
    }

    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        var imageUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);
        return string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("data:image/svg+xml", StringComparison.OrdinalIgnoreCase);
    }
}