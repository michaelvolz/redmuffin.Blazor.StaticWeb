using System.Text;
using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

public class OpenGraphImagesService : IOpenGraphImagesService
{
    private const string CacheNamespace = "opengraph_images";
    private const int CacheExpirationHours = 24;
    private readonly ICacheService _cacheService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageValidationService _imageValidationService;
    private readonly ILogger<OpenGraphImagesService> _logger;
    private static readonly Action<ILogger, string, Exception> LogFailureToRetrieveImageFromApi =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogFailureToRetrieveImageFromApi)),
            "Failed to retrieve image for article: {ArticleUrl}");
    private static readonly Action<ILogger, int, Exception?> LogMakingBatchApiCall =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, nameof(LogMakingBatchApiCall)),
            "Making batch API call for {Count} articles");
    private static readonly Action<ILogger, int, Exception?> LogParallelValidationCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, nameof(LogParallelValidationCompleted)),
            "Completed parallel validation and batch caching for {Count} images");
    private static readonly Action<ILogger, string, Exception> LogFailedToRetrieveCacheData =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(LogFailedToRetrieveCacheData)),
            "Failed to retrieve cache data for article: {ArticleUrl}, falling back to API");
    private static readonly Action<ILogger, int, Exception?> LogProcessingUniqueUrls =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(4, nameof(LogProcessingUniqueUrls)),
            "Processing {Count} unique URLs for batch image retrieval");
    private static readonly Action<ILogger, string, Exception?> LogCacheHit =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, nameof(LogCacheHit)),
            "Cache hit for URL: {Url}");
    private static readonly Action<ILogger, string, Exception?> LogCacheMiss =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, nameof(LogCacheMiss)),
            "Cache miss for URL: {Url}");
    private static readonly Action<ILogger, int, int, Exception?> LogMakingApiCallForUncachedUrls =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(7, nameof(LogMakingApiCallForUncachedUrls)),
            "Making API call for {Count} uncached URLs out of {Total} total URLs");
    private static readonly Action<ILogger, int, Exception?> LogAllUrlsFoundInCache =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(8, nameof(LogAllUrlsFoundInCache)),
            "All {Count} URLs found in cache, no API calls needed");
    private static readonly Action<ILogger, Exception> LogFailedToClearCache =
        LoggerMessage.Define(LogLevel.Error, new EventId(9, nameof(LogFailedToClearCache)),
            "Failed to clear cache");
    private static readonly Action<ILogger, Exception> LogFailedToGetCacheStatistics =
        LoggerMessage.Define(LogLevel.Error, new EventId(10, nameof(LogFailedToGetCacheStatistics)),
            "Failed to get cache statistics");
    private static readonly Action<ILogger, Exception> LogFailedToCleanupExpiredEntries =
        LoggerMessage.Define(LogLevel.Error, new EventId(11, nameof(LogFailedToCleanupExpiredEntries)),
            "Failed to cleanup expired entries");
    private static readonly Action<ILogger, string, Exception> LogFailedToUpdateCacheEntry =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(12, nameof(LogFailedToUpdateCacheEntry)),
            "Failed to update cache entry for article: {ArticleUrl}");
    private static readonly Action<ILogger, int, Exception?> LogStartingParallelValidation =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(13, nameof(LogStartingParallelValidation)),
            "Starting parallel validation of {Count} images");
    private static readonly Action<ILogger, string, string, Exception?> LogImageValidationSuccessful =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(14, nameof(LogImageValidationSuccessful)),
            "Image validation successful for {ArticleUrl}: {ImageUrl}");
    private static readonly Action<ILogger, string, string, string, Exception?> LogImageValidationFailed =
        LoggerMessage.Define<string, string, string>(LogLevel.Warning, new EventId(15, nameof(LogImageValidationFailed)),
            "Image validation failed for {ArticleUrl}: {ImageUrl} - {Error}");
    private static readonly Action<ILogger, Exception?> LogBatchApiResponseNoResults =
        LoggerMessage.Define(LogLevel.Warning, new EventId(16, nameof(LogBatchApiResponseNoResults)),
            "Batch API response contained no results");
    private static readonly Action<ILogger, System.Net.HttpStatusCode, Exception?> LogBatchApiCallFailed =
        LoggerMessage.Define<System.Net.HttpStatusCode>(LogLevel.Error, new EventId(17, nameof(LogBatchApiCallFailed)),
            "Batch API call failed with status code: {StatusCode}");
    private static readonly Action<ILogger, int, Exception> LogFailedToRetrieveImagesFromApi =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(18, nameof(LogFailedToRetrieveImagesFromApi)),
            "Failed to retrieve images from API for {Count} articles");
    private static readonly Action<ILogger, string, Exception> LogFailedToSaveCacheData =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(19, nameof(LogFailedToSaveCacheData)),
            "Failed to save cache data for article: {ArticleUrl}");
    private static readonly Action<ILogger, int, Exception?> LogStartingBatchCacheSave =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(20, nameof(LogStartingBatchCacheSave)),
            "Starting batch cache save for {Count} items");
    private static readonly Action<ILogger, string, Exception?> LogSuccessfullyCachedImageData =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(21, nameof(LogSuccessfullyCachedImageData)),
            "Successfully cached image data for article: {ArticleUrl}");
    private static readonly Action<ILogger, string, Exception?> LogSkippingCacheSaveForExpiredData =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(22, nameof(LogSkippingCacheSaveForExpiredData)),
            "Skipping cache save for expired data: {ArticleUrl}");
    private static readonly Action<ILogger, int, int, Exception?> LogCompletedBatchCacheSave =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(23, nameof(LogCompletedBatchCacheSave)),
            "Completed batch cache save for {Count} items with max concurrency {MaxConcurrency}");
    private static readonly Action<ILogger, int, Exception> LogFailedToBatchCacheSave =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(24, nameof(LogFailedToBatchCacheSave)),
            "Failed to perform batch cache save for {Count} items");
    private static readonly Action<ILogger, int, int, Exception?> LogStartingParallelImageValidation =
        LoggerMessage.Define<int, int>(LogLevel.Debug, new EventId(25, nameof(LogStartingParallelImageValidation)),
            "Starting parallel image validation for {Count} URLs with max concurrency {MaxConcurrency}");
    private static readonly Action<ILogger, string, string, long, Exception?> LogImageValidationSuccessfulDetails =
        LoggerMessage.Define<string, string, long>(LogLevel.Trace, new EventId(26, nameof(LogImageValidationSuccessfulDetails)),
            "Image validation successful: {ImageUrl} (Content-Type: {ContentType}, Size: {Size})");
    private static readonly Action<ILogger, string, string, Exception?> LogImageValidationFailedDetails =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(27, nameof(LogImageValidationFailedDetails)),
            "Image validation failed: {ImageUrl} - {Error}");
    private static readonly Action<ILogger, string, Exception?> LogNoValidationResultFound =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(28, nameof(LogNoValidationResultFound)),
            "No validation result found for image: {ImageUrl}");
    private static readonly Action<ILogger, int, int, int, Exception?> LogParallelImageValidationCompleted =
        LoggerMessage.Define<int, int, int>(LogLevel.Information, new EventId(29, nameof(LogParallelImageValidationCompleted)),
            "Parallel image validation completed: {ValidCount} valid, {InvalidCount} invalid out of {Total} total");
    private static readonly Action<ILogger, int, Exception> LogFailedToPerformParallelImageValidation =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(30, nameof(LogFailedToPerformParallelImageValidation)),
            "Failed to perform parallel image validation for {Count} URLs");

    public OpenGraphImagesService(
        IHttpClientFactory httpClientFactory,
        ICacheService cacheService,
        IImageValidationService imageValidationService,
        ILogger<OpenGraphImagesService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _imageValidationService = imageValidationService ?? throw new ArgumentNullException(nameof(imageValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CachedImageData?> GetImageAsync(string articleUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return null;

        try
        {
            // Check if the image is in cache
            var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, articleUrl, cancellationToken).ConfigureAwait(false);
            if (cacheData != null) return cacheData;
        }
        catch (Exception ex)
        {
            LogFailedToRetrieveCacheData(_logger, articleUrl, ex);
            // Continue to API call - don't fail the entire operation due to cache issues
        }

        // Not in cache or expired, retrieve from API
        return await GetImageFromCacheOrApiAsync(articleUrl, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, CachedImageData?>> GetImagesAsync(IEnumerable<string> articleUrls, CancellationToken cancellationToken = default)
    {
        if (articleUrls == null)
            return new Dictionary<string, CachedImageData?>();

        var urlList = articleUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        if (!urlList.Any())
            return new Dictionary<string, CachedImageData?>();

        LogProcessingUniqueUrls(_logger, urlList.Count, null);

        // Phase 1: Batch cache lookup for all URLs
        var result = new Dictionary<string, CachedImageData?>();
        var uncachedUrls = new List<string>();

        foreach (var url in urlList)
        {
            var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, url, cancellationToken).ConfigureAwait(false);
            if (cacheData != null)
            {
                result[url] = cacheData;
                LogCacheHit(_logger, url, null);
            }
            else
            {
                uncachedUrls.Add(url);
                LogCacheMiss(_logger, url, null);
            }
        }

        // Phase 2: Batch API call for uncached URLs only
        if (uncachedUrls.Any())
        {
            LogMakingApiCallForUncachedUrls(_logger, uncachedUrls.Count, urlList.Count, null);

            var batchResults = await GetImagesFromApiAsync(uncachedUrls, cancellationToken).ConfigureAwait(false);
            foreach (var kvp in batchResults) result[kvp.Key] = kvp.Value;
        }
        else
        {
            LogAllUrlsFoundInCache(_logger, urlList.Count, null);
        }

        return result;
    }

    public async Task<CachedImageData?> GetImageFromCacheOrApiAsync(string articleUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create API request to get image
            var httpClient = _httpClientFactory.CreateClient("DefaultHttpClient");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "redmuffin-blazor-staticweb/1.0");

            // Create batch request for single article
            var batchRequest = new BatchImageRequest
            {
                Articles = new List<ArticleImageRequest>
                {
                    new()
                    {
                        ArticleUrl = articleUrl,
                        ValidateImages = true,
                        MaxImages = 1
                    }
                },
                MaxConcurrency = 1,
                UseCache = false // We're handling cache at service level
            };

            var json = JsonSerializer.Serialize(batchRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call the Azure Function API
            var response = await httpClient.PostAsync("/api/GetOpenGraphImages", content, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var batchResponse = JsonSerializer.Deserialize<BatchImageResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (batchResponse?.Results?.Any() == true)
                {
                    var articleResult = batchResponse.Results.First();
                    if (articleResult.IsSuccess && !string.IsNullOrEmpty(articleResult.PrimaryImageUrl))
                    {
                        var cacheData = new CachedImageData
                        {
                            ArticleUrl = articleUrl,
                            ImageUrl = articleResult.PrimaryImageUrl,
                            ImageSource = articleResult.PrimaryImageSource,
                            IsValidated = true,
                            CachedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddHours(CacheExpirationHours),
                            LastAccessedAt = DateTime.UtcNow,
                            AccessCount = 1
                        };

                        await _cacheService.SetItemAsync(CacheNamespace, articleUrl, cacheData, CacheExpirationHours * 60, cancellationToken)
                            .ConfigureAwait(false);
                        return cacheData;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogFailureToRetrieveImageFromApi(_logger, articleUrl, ex);
        }

        return null;
    }

    public async Task<bool> IsImageCachedAsync(string articleUrl)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return false;

        var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, articleUrl).ConfigureAwait(false);
        return cacheData != null;
    }

    public async Task<bool> InvalidateCacheAsync(string articleUrl)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return false;

        try
        {
            await _cacheService.RemoveItemAsync(CacheNamespace, articleUrl).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            LogFailedToUpdateCacheEntry(_logger, articleUrl, ex);
            return false;
        }
    }

    public async Task<int> ClearCacheAsync()
    {
        try
        {
            await _cacheService.ClearNamespaceAsync(CacheNamespace).ConfigureAwait(false);
            return 0; // ClearNamespaceAsync returns void, we can't count removed items
        }
        catch (Exception ex)
        {
            LogFailedToClearCache(_logger, ex);
            return 0;
        }
    }

    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        try
        {
            var stats = await _cacheService.GetNamespaceStatsAsync(CacheNamespace).ConfigureAwait(false);
            return new Dictionary<string, object>
            {
                ["Namespace"] = stats.Namespace,
                ["TotalItems"] = stats.TotalItems,
                ["TotalSizeBytes"] = stats.TotalSizeBytes,
                ["ExpiredItemsCount"] = stats.ExpiredItemsCount,
                ["OldestItemTimestamp"] = stats.OldestItemTimestamp?.ToString() ?? "N/A",
                ["NewestItemTimestamp"] = stats.NewestItemTimestamp?.ToString() ?? "N/A",
                ["AverageAccessCount"] = stats.AverageAccessCount
            };
        }
        catch (Exception ex)
        {
            LogFailedToGetCacheStatistics(_logger, ex);
            return new Dictionary<string, object>();
        }
    }

    public async Task<int> CleanupExpiredEntriesAsync()
    {
        try
        {
            return await _cacheService.CleanupExpiredItemsAsync(CacheNamespace).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogFailedToCleanupExpiredEntries(_logger, ex);
            return 0;
        }
    }

    public async Task<bool> UpdateCacheEntryAsync(string articleUrl, CachedImageData imageData)
    {
        if (string.IsNullOrWhiteSpace(articleUrl) || imageData == null)
            return false;

        try
        {
            var expiration = imageData.ExpiresAt - DateTime.UtcNow;
            if (expiration > TimeSpan.Zero)
                await _cacheService.SetItemAsync(CacheNamespace, articleUrl, imageData, (int)expiration.TotalMinutes).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            LogFailedToUpdateCacheEntry(_logger, articleUrl, ex);
            return false;
        }
    }

    /// <summary>
    ///     Retrieves images from the API for multiple URLs in a single batch request,
    ///     then validates them in parallel with proper error handling.
    /// </summary>
    /// <param name="articleUrls">List of article URLs to process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Dictionary mapping article URLs to their cached image data</returns>
    private async Task<Dictionary<string, CachedImageData?>> GetImagesFromApiAsync(IList<string> articleUrls, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, CachedImageData?>();

        if (!articleUrls.Any())
            return result;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("DefaultHttpClient");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "redmuffin-blazor-staticweb/1.0");

            // Create batch request for multiple articles
            var batchRequest = new BatchImageRequest
            {
                Articles = articleUrls.Select(url => new ArticleImageRequest
                {
                    ArticleUrl = url,
                    ValidateImages = false, // We'll validate separately for better control
                    MaxImages = 1
                }).ToList(),
                MaxConcurrency = Math.Min(articleUrls.Count, 5), // Limit concurrent requests
                UseCache = false // We're handling cache at service level,
            };

            var json = JsonSerializer.Serialize(batchRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            LogMakingBatchApiCall(_logger, articleUrls.Count, null);

            // Call the Azure Function API
            var response = await httpClient.PostAsync("/api/GetOpenGraphImages", content, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var batchResponse = JsonSerializer.Deserialize<BatchImageResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (batchResponse?.Results?.Any() == true)
                {
                    // Phase 1: Collect successful results for validation
                    var imagesToValidate = new List<(string ArticleUrl, string ImageUrl, ImageSource ImageSource)>();

                    foreach (var articleResult in batchResponse.Results)
                        if (articleResult.IsSuccess && !string.IsNullOrEmpty(articleResult.PrimaryImageUrl))
                        {
                            imagesToValidate.Add((articleResult.ArticleUrl, articleResult.PrimaryImageUrl, articleResult.PrimaryImageSource));
                        }
                        else
                        {
                            // Cache failed results immediately to avoid repeated API calls
                            var failedCacheData = new CachedImageData
                            {
                                ArticleUrl = articleResult.ArticleUrl,
                                ImageUrl = string.Empty,
                                ImageSource = ImageSource.None,
                                IsValidated = false,
                                CachedAt = DateTime.UtcNow,
                                ExpiresAt = DateTime.UtcNow.AddHours(2), // Shorter cache for failed results
                                LastAccessedAt = DateTime.UtcNow,
                                AccessCount = 1
                            };

                            await SaveImageToCacheAsync(failedCacheData).ConfigureAwait(false);
                            result[articleResult.ArticleUrl] = null;
                        }

                    // Phase 2: Validate images in parallel
                    if (imagesToValidate.Any())
                    {
                        LogStartingParallelValidation(_logger, imagesToValidate.Count, null);

                        var validationResults = await ValidateImagesInParallelAsync(
                            imagesToValidate.Select(x => x.ImageUrl).ToList(),
                            cancellationToken).ConfigureAwait(false);

                        // Phase 3: Process validation results and prepare batch cache updates
                        var cacheDataBatch = new List<CachedImageData>();

                        foreach (var (articleUrl, imageUrl, imageSource) in imagesToValidate)
                        {
                            var validationResult = validationResults.GetValueOrDefault(imageUrl);

                            if (validationResult?.IsValid == true)
                            {
                                // Valid image - cache with full expiration
                                var cacheData = new CachedImageData
                                {
                                    ArticleUrl = articleUrl,
                                    ImageUrl = imageUrl,
                                    ImageSource = imageSource,
                                    IsValidated = true,
                                    CachedAt = DateTime.UtcNow,
                                    ExpiresAt = DateTime.UtcNow.AddHours(24), // Cache for 24 hours
                                    LastAccessedAt = DateTime.UtcNow,
                                    AccessCount = 1
                                };

                                cacheDataBatch.Add(cacheData);
                                result[articleUrl] = cacheData;

                                LogImageValidationSuccessful(_logger, articleUrl, imageUrl, null);
                            }
                            else
                            {
                                // Invalid image - cache with shorter expiration
                                var invalidCacheData = new CachedImageData
                                {
                                    ArticleUrl = articleUrl,
                                    ImageUrl = imageUrl,
                                    ImageSource = imageSource,
                                    IsValidated = false,
                                    CachedAt = DateTime.UtcNow,
                                    ExpiresAt = DateTime.UtcNow.AddHours(6), // Shorter cache for invalid images
                                    LastAccessedAt = DateTime.UtcNow,
                                    AccessCount = 1
                                };

                                cacheDataBatch.Add(invalidCacheData);
                                result[articleUrl] = null;

                                LogImageValidationFailed(_logger, articleUrl, imageUrl, validationResult?.ErrorMessage ?? "Unknown error", null);
                            }
                        }

                        // Execute batch cache updates for improved performance
                        await SaveImageBatchToCacheAsync(cacheDataBatch, cancellationToken).ConfigureAwait(false);

                        LogParallelValidationCompleted(_logger, imagesToValidate.Count, null);
                    }
                }
                else
                {
                    LogBatchApiResponseNoResults(_logger, null);
                }
            }
            else
            {
                LogBatchApiCallFailed(_logger, response.StatusCode, null);
            }
        }
        catch (Exception ex)
        {
            LogFailedToRetrieveImagesFromApi(_logger, articleUrls.Count, ex);
        }

        // Ensure all requested URLs have entries in the result, even if they failed
        foreach (var url in articleUrls)
            if (!result.ContainsKey(url))
                result[url] = null;

        return result;
    }

    private async Task SaveImageToCacheAsync(CachedImageData cacheData)
    {
        try
        {
            var expiration = cacheData.ExpiresAt - DateTime.UtcNow;
            if (expiration > TimeSpan.Zero)
                await _cacheService.SetItemAsync(CacheNamespace, cacheData.ArticleUrl, cacheData, (int)expiration.TotalMinutes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogFailedToSaveCacheData(_logger, cacheData.ArticleUrl, ex);
        }
    }

    /// <summary>
    ///     Saves multiple images to cache in batch for improved performance.
    ///     Uses parallel processing with controlled concurrency to avoid overwhelming the storage system.
    /// </summary>
    /// <param name="cacheDataBatch">List of cache data to save</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the batch cache operation</returns>
    private async Task SaveImageBatchToCacheAsync(IList<CachedImageData> cacheDataBatch, CancellationToken cancellationToken = default)
    {
        if (!cacheDataBatch.Any())
            return;

        try
        {
            LogStartingBatchCacheSave(_logger, cacheDataBatch.Count, null);

            // Use controlled concurrency to avoid overwhelming the browser storage
            var maxConcurrency = Math.Min(cacheDataBatch.Count, 10); // Limit concurrent cache operations
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var cacheTasks = cacheDataBatch.Select(async cacheData =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var expiration = cacheData.ExpiresAt - DateTime.UtcNow;
                    if (expiration > TimeSpan.Zero)
                    {
                        await _cacheService.SetItemAsync(CacheNamespace, cacheData.ArticleUrl, cacheData, (int)expiration.TotalMinutes, cancellationToken)
                            .ConfigureAwait(false);
                        LogSuccessfullyCachedImageData(_logger, cacheData.ArticleUrl, null);
                    }
                    else
                    {
                        LogSkippingCacheSaveForExpiredData(_logger, cacheData.ArticleUrl, null);
                    }
                }
                catch (Exception ex)
                {
                    LogFailedToSaveCacheData(_logger, cacheData.ArticleUrl, ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(cacheTasks).ConfigureAwait(false);

            LogCompletedBatchCacheSave(_logger, cacheDataBatch.Count, maxConcurrency, null);
        }
        catch (Exception ex)
        {
            LogFailedToBatchCacheSave(_logger, cacheDataBatch.Count, ex);
        }
    }


    /// <summary>
    ///     Validates multiple image URLs in parallel using the image validation service.
    /// </summary>
    /// <param name="imageUrls">List of image URLs to validate</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Dictionary mapping image URLs to their validation results</returns>
    private async Task<Dictionary<string, ImageValidationResult>> ValidateImagesInParallelAsync(
        IList<string> imageUrls,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ImageValidationResult>();

        if (!imageUrls.Any())
            return result;

        try
        {
            // Use the image validation service with controlled concurrency
            var maxConcurrency = Math.Min(imageUrls.Count, 8); // Limit concurrent validations

            LogStartingParallelImageValidation(_logger, imageUrls.Count, maxConcurrency, null);

            // Call the validation service which handles parallel processing internally
            var validationResults = await _imageValidationService.ValidateImagesAsync(
                imageUrls,
                maxConcurrency,
                cancellationToken).ConfigureAwait(false);

            // Process results with comprehensive error handling
            foreach (var imageUrl in imageUrls)
                if (validationResults.TryGetValue(imageUrl, out var validationResult))
                {
                    result[imageUrl] = validationResult;

                    if (validationResult.IsValid)
                        LogImageValidationSuccessfulDetails(_logger, imageUrl, validationResult.ContentType ?? "Unknown", validationResult.ContentLength ?? 0, null);
                    else
                        LogImageValidationFailedDetails(_logger, imageUrl, validationResult.ErrorMessage ?? "Unknown error", null);
                }
                else
                {
                    // Create a failed validation result for missing entries
                    var failedResult = new ImageValidationResult
                    {
                        ImageUrl = imageUrl,
                        IsValid = false,
                        ErrorMessage = "Validation result not found",
                        ValidatedAt = DateTime.UtcNow
                    };

                    result[imageUrl] = failedResult;
                    LogNoValidationResultFound(_logger, imageUrl, null);
                }

            var validCount = result.Values.Count(v => v.IsValid);
            var invalidCount = result.Values.Count - validCount;

            LogParallelImageValidationCompleted(_logger, validCount, invalidCount, imageUrls.Count, null);
        }
        catch (Exception ex)
        {
            LogFailedToPerformParallelImageValidation(_logger, imageUrls.Count, ex);

            // Fallback: Mark all images as invalid to avoid blocking the process
            foreach (var imageUrl in imageUrls)
                if (!result.ContainsKey(imageUrl))
                    result[imageUrl] = new ImageValidationResult
                    {
                        ImageUrl = imageUrl,
                        IsValid = false,
                        ErrorMessage = $"Validation failed due to system error: {ex.Message}",
                        ValidatedAt = DateTime.UtcNow
                    };
        }

        return result;
    }
}