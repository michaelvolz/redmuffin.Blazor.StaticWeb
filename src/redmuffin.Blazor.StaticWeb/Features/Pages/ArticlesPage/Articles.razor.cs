using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Models;
using redmuffin.Blazor.StaticWeb.Services;

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

    private static readonly Action<ILogger, int, int, Exception?> LogImageProcessingStarted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(4, nameof(LogImageProcessingStarted)),
            "Started processing OpenGraph images for {ProcessingCount} out of {TotalCount} articles");

    private static readonly Action<ILogger, int, int, Exception?> LogImageProcessingCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(5, nameof(LogImageProcessingCompleted)),
            "Completed processing OpenGraph images: {SuccessCount} successful, {FailedCount} failed");

    private static readonly Action<ILogger, Exception?> LogNoArticlesRequireProcessing =
        LoggerMessage.Define(LogLevel.Debug, new EventId(6, nameof(LogNoArticlesRequireProcessing)),
            "No articles require OpenGraph image processing");

    private static readonly Action<ILogger, Exception> LogProcessingOpenGraphImagesError =
        LoggerMessage.Define(LogLevel.Error, new EventId(7, nameof(LogProcessingOpenGraphImagesError)),
            "Error processing OpenGraph images");

    private static readonly Action<ILogger, string, Exception> LogImageLoadEventError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(8, nameof(LogImageLoadEventError)),
            "Error handling image load event for article: {ArticleLink}");

    private static readonly Action<ILogger, string, string, Exception> LogImageValidationError =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(9, nameof(LogImageValidationError)),
            "Error validating image for article: {ArticleLink}, ImageUrl: {ImageUrl}");

    private static readonly Action<ILogger, string, Exception> LogCacheUpdateError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(10, nameof(LogCacheUpdateError)),
            "Error updating cache with validation result for article: {ArticleLink}");

    private readonly Dictionary<string, ArticleProcessingState> _articleStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.OrdinalIgnoreCase);
    private List<RaindropItem>? _articleItems;

    private string? _errorMessage;
    private bool _isLoading;
    private bool _isProcessingImages;
    private int _processingCount;

    [Inject]
    private ILogger<Articles> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IOpenGraphImagesService OpenGraphImagesService { get; set; } = null!;

    [Inject]
    private IImageValidationService ImageValidationService { get; set; } = null!;

    /// <summary>
    ///     Determines if a cover image URL is suspicious and likely needs replacement.
    /// </summary>
    /// <param name="coverUrl">The cover image URL to evaluate</param>
    /// <returns>True if the image is suspicious and should be replaced</returns>
    private static bool IsCoverImageSuspicious(string coverUrl)
    {
        if (string.IsNullOrEmpty(coverUrl))
            return true;

        // Check for common placeholder patterns
        var suspiciousPatterns = new[]
        {
            "placeholder",
            "default",
            "no-image",
            "missing",
            "avatar",
            "profile",
            "blank",
            "generic",
            "thumb",
            "1x1",
            "pixel"
        };

        var lowerUrl = coverUrl.ToLowerInvariant();
        return suspiciousPatterns.Any(pattern => lowerUrl.Contains(pattern));
    }

    /// <summary>
    ///     Determines if an article should have its image enhanced even if it has a cover image.
    /// </summary>
    /// <param name="article">The article to evaluate</param>
    /// <returns>True if the article should be enhanced</returns>
    private static bool ShouldEnhanceImage(RaindropItem article)
    {
        // Always try to enhance if no cover image
        if (string.IsNullOrEmpty(article.Cover))
            return true;

        // Enhance articles from specific domains known to have better OpenGraph images
        var domainsToEnhance = new[]
        {
            "github.com",
            "medium.com",
            "dev.to",
            "hashnode.com",
            "stackoverflow.com",
            "docs.microsoft.com",
            "devblogs.microsoft.com"
        };

        return domainsToEnhance.Any(domain => article.Link.Contains(domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the CSS class for the article card based on its processing state.
    /// </summary>
    /// <param name="processingState">The processing state of the article</param>
    /// <returns>CSS class name for the card state</returns>
    private static string GetCardStateClass(string processingState)
    {
        return processingState switch
        {
            "processing" => "image-processing",
            "enhanced" => "image-enhanced",
            "failed" => "image-failed",
            _ => string.Empty
        };
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
        ArgumentNullException.ThrowIfNull(OpenGraphImagesService);
        ArgumentNullException.ThrowIfNull(ImageValidationService);

        // Load articles automatically when the page starts
        await FetchArticlesAsync().ConfigureAwait(false);
    }

    private async Task FetchArticlesAsync()
    {
        _errorMessage = null;
        _articleItems = null;
        _isLoading = true;
        StateHasChanged();

        try
        {
            var response = await Http.GetAsync("/api/RaindropListArticles").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                LogRawJsonResponse(Logger, json, null);

                try
                {
                    // Use JsonTypeInfo for deserialization to avoid trimming issues
                    _articleItems = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList);

                    // Process OpenGraph images for articles requiring enhancement
                    if (_articleItems is { Count: > 0 })
                    {
                        // Populate image URL cache for initial render
                        await PopulateImageUrlCacheAsync().ConfigureAwait(false);
                        StateHasChanged();

                        // Don't await - process images in background
                        _ = ProcessOpenGraphImagesAsync();
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
                    return;
                }
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

    /// <summary>
    ///     Populates the image URL cache with the best available image URLs for all articles.
    ///     This ensures images are displayed immediately on initial render.
    /// </summary>
    private async Task PopulateImageUrlCacheAsync()
    {
        if (_articleItems == null) return;

        foreach (var article in _articleItems)
        {
            var imageUrl = await GetBestImageUrlAsync(article).ConfigureAwait(false);
            
            // Check if this image has been previously blocked by the browser
            var validationResult = await ImageValidationService.ValidateImageWithCacheAsync(imageUrl).ConfigureAwait(false);
            
            if (!validationResult.IsValid && validationResult.ErrorMessage != null && 
                validationResult.ErrorMessage.Contains("Browser blocked", StringComparison.OrdinalIgnoreCase))
            {
                // This image was previously blocked, use data URI placeholder instead
                _imageUrlCache[article.Link] = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KICA8cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjVmNWY1IiBzdHJva2U9IiNkZGQiIHN0cm9rZS13aWR0aD0iMiIvPgogIDx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBkb21pbmFudC1iYXNlbGluZT0ibWlkZGxlIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LWZhbWlseT0iQXJpYWwsIHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTYiIGZpbGw9IiM5OTkiPkltYWdlIEJsb2NrZWQ8L3RleHQ+Cjwvc3ZnPg==";
            }
            else
            {
                _imageUrlCache[article.Link] = imageUrl;
            }
        }
    }

    /// <summary>
    ///     Processes OpenGraph images for articles that require image enhancement.
    ///     This method identifies articles that need better images and processes them in batches.
    /// </summary>
    private async Task ProcessOpenGraphImagesAsync()
    {
        if (_articleItems == null || _isProcessingImages)
            return;

        _isProcessingImages = true;

        try
        {
            // Identify articles requiring image processing
            var articlesRequiringProcessing = await IdentifyArticlesRequiringImageProcessingAsync(_articleItems).ConfigureAwait(false);

            if (!(articlesRequiringProcessing.Count > 0))
            {
                LogNoArticlesRequireProcessing(Logger, null);
                return;
            }

            // Initialize tracking and states
            InitializeProcessingTracking(articlesRequiringProcessing.Count, _articleItems.Count);
            InitializeArticleStates(articlesRequiringProcessing);
            StateHasChanged();

            // Process images
            var urlsToProcess = articlesRequiringProcessing.Select(a => a.Link).ToList();
            var results = await OpenGraphImagesService.GetImagesAsync(urlsToProcess).ConfigureAwait(false);

            // Process results
            var (successCount, failedCount) = ProcessImageResults(results);

            LogImageProcessingCompleted(Logger, successCount, failedCount, null);

            // Final UI update
            StateHasChanged();
        }
        catch (Exception ex)
        {
            HandleProcessingError(ex);
        }
        finally
        {
            _isProcessingImages = false;
        }
    }

    /// <summary>
    ///     Initializes progress tracking for image processing.
    /// </summary>
    private void InitializeProcessingTracking(int processingCount, int totalCount)
    {
        _processingCount = processingCount;
        LogImageProcessingStarted(Logger, processingCount, totalCount, null);
    }

    /// <summary>
    ///     Initializes article processing states.
    /// </summary>
    private void InitializeArticleStates(List<RaindropItem> articles)
    {
        foreach (var article in articles)
        {
            var state = GetOrCreateArticleState(article.Link);
            state.StartProcessing();
        }
    }

    /// <summary>
    ///     Processes image results and updates article states.
    /// </summary>
    private (int SuccessCount, int FailedCount) ProcessImageResults(IDictionary<string, CachedImageData?> results)
    {
        var successCount = 0;
        var failedCount = 0;
        var processedCount = 0;

        foreach (var result in results)
        {
            var state = GetOrCreateArticleState(result.Key);

            if (result.Value?.IsValidated == true && !string.IsNullOrEmpty(result.Value.ImageUrl))
            {
                state.CompleteProcessing(result.Value);

                // Update the image URL cache with the new enhanced image
                _imageUrlCache[result.Key] = result.Value.ImageUrl;

                successCount++;
            }
            else
            {
                var errorMessage = "Unknown processing error";
                var fallbackReason = DetermineFallbackReason(result.Key, result.Value);
                state.FailProcessing(errorMessage, fallbackReason);
                failedCount++;
            }

            // Update progress
            processedCount++;

            // Trigger incremental UI updates for smooth progress
            if (processedCount % 2 == 0 || processedCount == _processingCount) StateHasChanged();
        }

        return (successCount, failedCount);
    }

    /// <summary>
    ///     Handles processing errors by updating states and UI.
    /// </summary>
    private void HandleProcessingError(Exception ex)
    {
        LogProcessingOpenGraphImagesError(Logger, ex);

        // Update all processing states to failed
        foreach (var state in _articleStates.Values.Where(state => state.ProcessingPhase == ProcessingPhase.Processing))
            state.FailProcessing(ex.Message, "Processing error");

        StateHasChanged();
    }

    /// <summary>
    ///     Identifies articles that require OpenGraph image processing.
    ///     Articles need processing if they have broken/missing cover images or can benefit from enhancement.
    /// </summary>
    /// <param name="articles">List of articles to evaluate</param>
    /// <returns>List of articles that need image processing</returns>
    private async Task<List<RaindropItem>> IdentifyArticlesRequiringImageProcessingAsync(List<RaindropItem> articles)
    {
        var articlesRequiringProcessing = new List<RaindropItem>();

        foreach (var article in articles)
        {
            // Skip if already processed or processing
            if (_articleStates.TryGetValue(article.Link, out var state) &&
                state.ProcessingPhase != ProcessingPhase.None)
                continue;

            // Check if already cached in local storage to avoid unnecessary reprocessing
            var isCached = await OpenGraphImagesService.IsImageCachedAsync(article.Link).ConfigureAwait(false);
            if (isCached)
            {
                // Update state to reflect that we have cached data
                var cachedState = GetOrCreateArticleState(article.Link);
                cachedState.ProcessingPhase = ProcessingPhase.Completed;
                continue;
            }

            // Process if:
            // 1. No cover image exists
            // 2. Cover image is likely broken (generic placeholder patterns)
            // 3. Cover image is from a CDN known to have issues
            var needsProcessing = string.IsNullOrEmpty(article.Cover) ||
                                  IsCoverImageSuspicious(article.Cover) ||
                                  ShouldEnhanceImage(article);

            if (needsProcessing) articlesRequiringProcessing.Add(article);
        }

        return articlesRequiringProcessing;
    }

    /// <summary>
    ///     Gets the best available image URL for an article, prioritizing enhanced images.
    /// </summary>
    /// <param name="article">The article to get the image for</param>
    /// <returns>The best available image URL</returns>
    private async Task<string> GetBestImageUrlAsync(RaindropItem article)
    {
        // First, try to get enhanced image from state
        if (_articleStates.TryGetValue(article.Link, out var state) &&
            state.EnhancedImage?.IsValidated == true &&
            !string.IsNullOrEmpty(state.EnhancedImage.ImageUrl))
            return state.EnhancedImage.ImageUrl;

        // Check if we have a cached image in local storage
        var cachedImage = await OpenGraphImagesService.GetImageAsync(article.Link).ConfigureAwait(false);
        if (cachedImage?.IsValidated == true && !string.IsNullOrEmpty(cachedImage.ImageUrl))
        {
            // Update state with cached data
            state = GetOrCreateArticleState(article.Link);
            state.EnhancedImage = cachedImage;
            state.ProcessingPhase = ProcessingPhase.Completed;
            return cachedImage.ImageUrl;
        }

        // Fallback to original cover image if available
        return !string.IsNullOrEmpty(article.Cover)
            ? article.Cover
            : "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KICA8cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjVmNWY1IiBzdHJva2U9IiNkZGQiIHN0cm9rZS13aWR0aD0iMiIvPgogIDx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBkb21pbmFudC1iYXNlbGluZT0ibWlkZGxlIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LWZhbWlseT0iQXJpYWwsIHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTYiIGZpbGw9IiM5OTkiPk5vIEltYWdlIEF2YWlsYWJsZTwvdGV4dD4KPC9zdmc+";
    }

    /// <summary>
    ///     Gets the processing state for an article's image.
    /// </summary>
    /// <param name="article">The article to get the state for</param>
    /// <returns>Processing state: "processing", "enhanced", "failed", or "none"</returns>
    private string GetImageProcessingState(RaindropItem article)
    {
        if (_articleStates.TryGetValue(article.Link, out var state))
            return state.ProcessingPhase switch
            {
                ProcessingPhase.Processing => "processing",
                ProcessingPhase.Completed => "enhanced",
                ProcessingPhase.Failed => "failed",
                _ => "none"
            };
        return "none";
    }

    /// <summary>
    ///     Handles image load/error events and updates load states for fallback placeholder management.
    ///     This method integrates image validation and cache updates with UI rendering.
    /// </summary>
    /// <param name="elementId">The ID of the shimmer element</param>
    /// <param name="articleLink">The link of the article</param>
    /// <param name="loadSuccess">Whether the image loaded successfully</param>
    private async Task HandleImageLoadAsync(string elementId, string articleLink, bool loadSuccess)
    {
        try
        {
            // Get or create article state
            var state = GetOrCreateArticleState(articleLink);

            // Update image load state
            state.SetImageLoadState(loadSuccess ? ImageLoadState.Loaded : ImageLoadState.Failed);

            // Get the current image URL being displayed
            var article = _articleItems?.FirstOrDefault(a => string.Equals(a.Link, articleLink, StringComparison.Ordinal));
            if (article != null)
            {
                var currentImageUrl = await GetBestImageUrlAsync(article).ConfigureAwait(false);

                // Validate the image in the background and update cache
                _ = ValidateAndUpdateImageCacheAsync(currentImageUrl, articleLink, loadSuccess);
            }

            // Handle load failure scenarios
            if (!loadSuccess)
            {
                var fallbackReason = DetermineFallbackReason(articleLink);
                state.SetFallbackReason(fallbackReason);

                // Update validation state to reflect failure
                state.ValidationState = ImageValidationState.Failed;
            }
            else
            {
                // Image loaded successfully, update validation state
                state.ValidationState = ImageValidationState.Validated;
            }

            // Stop shimmer effect
            await StopShimmerAsync(elementId).ConfigureAwait(false);

            // Update UI to reflect new state
            StateHasChanged();
        }
        catch (Exception ex)
        {
            LogImageLoadEventError(Logger, articleLink, ex);
        }
    }

    /// <summary>
    ///     Validates an image URL and updates the cache with validation results.
    ///     This method runs in the background to maintain UI responsiveness.
    /// </summary>
    /// <param name="imageUrl">The image URL to validate</param>
    /// <param name="articleLink">The article link associated with the image</param>
    /// <param name="loadSuccess">Whether the image loaded successfully in the browser</param>
    private async Task ValidateAndUpdateImageCacheAsync(string imageUrl, string articleLink, bool loadSuccess)
    {
        try
        {
            // Skip validation for placeholder URLs
            if (imageUrl.Contains("placeholder.com", StringComparison.OrdinalIgnoreCase)) return;

            // Get article state for updates
            var state = GetOrCreateArticleState(articleLink);

            // If browser failed to load the image, record it as blocked instead of attempting HTTP validation
            if (!loadSuccess)
            {
                // Record this as a browser-blocked image to prevent future HTTP validation attempts
                await ImageValidationService.RecordBrowserBlockedImageAsync(imageUrl, "Browser load failed (likely CORS/SameSite blocking)").ConfigureAwait(false);

                state.ValidationState = ImageValidationState.Failed;
                state.SetFallbackReason("Browser blocked image (CORS/SameSite policy)");

                StateHasChanged();
                return;
            }

            // Update validation state to indicate validation is in progress
            state.ValidationState = ImageValidationState.Validating;

            // Perform HTTP HEAD validation with caching
            var validationResult = await ImageValidationService.ValidateImageWithCacheAsync(imageUrl).ConfigureAwait(false);

            // Update article state based on validation result
            if (validationResult.IsValid)
            {
                state.ValidationState = ImageValidationState.Validated;

                // Update cache with validation confirmation
            }
            else
            {
                state.ValidationState = ImageValidationState.Failed;

                // If validation failed but browser load succeeded, mark as suspicious
                if (loadSuccess) state.SetFallbackReason("Image validation failed: " + validationResult.ErrorMessage);

                // Update cache to reflect validation failure
            }

            await UpdateCacheWithValidationResultAsync(articleLink, validationResult).ConfigureAwait(false);

            // Trigger UI update to reflect validation state changes
            StateHasChanged();
        }
        catch (Exception ex)
        {
            LogImageValidationError(Logger, articleLink, imageUrl, ex);

            // Update state to reflect validation error
            var state = GetOrCreateArticleState(articleLink);
            state.ValidationState = ImageValidationState.Failed;
            state.SetFallbackReason("Validation error: " + ex.Message);

            StateHasChanged();
        }
    }

    /// <summary>
    ///     Updates the cache with validation results to improve future performance.
    /// </summary>
    /// <param name="articleLink">The article link associated with the image</param>
    /// <param name="validationResult">The validation result</param>
    private async Task UpdateCacheWithValidationResultAsync(string articleLink, ImageValidationResult validationResult)
    {
        try
        {
            // Check if this is an enhanced image from OpenGraph processing
            if (_articleStates.TryGetValue(articleLink, out var state) && state.EnhancedImage != null)
            {
                // Update the cached image data with validation results
                state.EnhancedImage.IsValidated = validationResult.IsValid;
                state.EnhancedImage.LastAccessedAt = DateTime.UtcNow;
                state.EnhancedImage.AccessCount++;

                // Update the cache through the OpenGraph service
                if (validationResult.IsValid)
                    await OpenGraphImagesService.UpdateCacheEntryAsync(articleLink, state.EnhancedImage).ConfigureAwait(false);
                else
                    // If validation failed, consider invalidating the cache entry
                    await OpenGraphImagesService.InvalidateCacheAsync(articleLink).ConfigureAwait(false);
            }

            // The ImageValidationService handles its own cache management
            // No additional action needed as ValidateImageWithCacheAsync already caches the result
        }
        catch (Exception ex)
        {
            LogCacheUpdateError(Logger, articleLink, ex);
        }
    }

    /// <summary>
    ///     Determines if an article should show a fallback placeholder.
    /// </summary>
    /// <param name="article">The article to check</param>
    /// <returns>True if fallback placeholder should be shown</returns>
    private bool HasFallbackPlaceholder(RaindropItem article)
    {
        if (_articleStates.TryGetValue(article.Link, out var state))
        {
            // Show fallback if image failed to load
            if (state.ImageLoadState == ImageLoadState.Failed) return true;

            // Show fallback if processing failed and no original cover image
            if (state.ProcessingPhase == ProcessingPhase.Failed &&
                string.IsNullOrEmpty(article.Cover))
                return true;
        }

        // Show fallback if using placeholder URL
        var imageUrl = _imageUrlCache.GetValueOrDefault(article.Link, string.Empty);

        return string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("placeholder.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Gets the fallback reason message for an article.
    /// </summary>
    /// <param name="article">The article to get the reason for</param>
    /// <returns>Fallback reason message</returns>
    private string GetFallbackReason(RaindropItem article)
    {
        // Return cached reason if available from state
        if (_articleStates.TryGetValue(article.Link, out var state) &&
            !string.IsNullOrEmpty(state.FallbackReason))
            return state.FallbackReason;

        // Determine reason based on current state
        var reason = DetermineFallbackReason(article.Link);
        state?.SetFallbackReason(reason);
        return reason;
    }

    /// <summary>
    ///     Gets or creates an article processing state for the given article link.
    /// </summary>
    /// <param name="articleLink">The article link to get or create state for</param>
    /// <returns>The article processing state</returns>
    private ArticleProcessingState GetOrCreateArticleState(string articleLink)
    {
        if (_articleStates.TryGetValue(articleLink, out var state)) return state;

        state = new ArticleProcessingState();
        _articleStates[articleLink] = state;

        return state;
    }

    /// <summary>
    ///     Determines the fallback reason for an article based on its current state.
    /// </summary>
    /// <param name="articleLink">The article link</param>
    /// <param name="cachedImageData">The cached image data if available</param>
    /// <returns>Fallback reason message</returns>
    private string DetermineFallbackReason(string articleLink, CachedImageData? cachedImageData = null)
    {
        // If we have cached data, use it to determine the reason
        if (cachedImageData != null)
        {
            if (!cachedImageData.IsValidated) return "Image not verified";

            if (string.IsNullOrEmpty(cachedImageData.ImageUrl)) return "No image found";
        }

        // Check article state for more specific reasons
        if (_articleStates.TryGetValue(articleLink, out var articleState))
        {
            if (articleState.ProcessingPhase == ProcessingPhase.Failed) return articleState.ErrorMessage ?? "Enhancement failed";

            if (articleState.ImageLoadState == ImageLoadState.Failed) return "Image unavailable";
        }

        // Find the article to check original cover
        var article = _articleItems?.FirstOrDefault(a => string.Equals(a.Link, articleLink, StringComparison.Ordinal));
        if (article == null) return "Image not available";

        if (string.IsNullOrEmpty(article.Cover)) return "No image available";

        return IsCoverImageSuspicious(article.Cover) ? "Placeholder image" : "Image not available";
    }
}