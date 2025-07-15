using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

public class OpenGraphImagesService : IOpenGraphImagesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;
    private readonly IImageValidationService _imageValidationService;
    private readonly ILogger<OpenGraphImagesService> _logger;
    private const string CacheNamespace = "opengraph_images";
    private const int CacheExpirationHours = 24;

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
            var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, articleUrl);
            if (cacheData != null)
            {
                return cacheData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cache data for article: {ArticleUrl}, falling back to API", articleUrl);
            // Continue to API call - don't fail the entire operation due to cache issues
        }

        // Not in cache or expired, retrieve from API
        return await GetImageFromCacheOrApiAsync(articleUrl, cancellationToken);
    }

    public async Task<Dictionary<string, CachedImageData?>> GetImagesAsync(IEnumerable<string> articleUrls, CancellationToken cancellationToken = default)
    {
        if (articleUrls == null)
            return new Dictionary<string, CachedImageData?>();
            
        var urlList = articleUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        if (!urlList.Any())
            return new Dictionary<string, CachedImageData?>();

        _logger.LogDebug("Processing {Count} unique URLs for batch image retrieval", urlList.Count);

        // Phase 1: Batch cache lookup for all URLs
        var result = new Dictionary<string, CachedImageData?>();
        var uncachedUrls = new List<string>();

        foreach (var url in urlList)
        {
            var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, url, cancellationToken);
            if (cacheData != null)
            {
                result[url] = cacheData;
                _logger.LogDebug("Cache hit for URL: {Url}", url);
            }
            else
            {
                uncachedUrls.Add(url);
                _logger.LogDebug("Cache miss for URL: {Url}", url);
            }
        }

        // Phase 2: Batch API call for uncached URLs only
        if (uncachedUrls.Any())
        {
            _logger.LogInformation("Making API call for {Count} uncached URLs out of {Total} total URLs", 
                uncachedUrls.Count, urlList.Count);
            
            var batchResults = await GetImagesFromApiAsync(uncachedUrls, cancellationToken);
            foreach (var kvp in batchResults)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            _logger.LogInformation("All {Count} URLs found in cache, no API calls needed", urlList.Count);
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
                    new ArticleImageRequest
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
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Call the Azure Function API
            var response = await httpClient.PostAsync("/api/GetOpenGraphImages", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
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

                        await _cacheService.SetItemAsync(CacheNamespace, articleUrl, cacheData, CacheExpirationHours * 60);
                        return cacheData;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve image for article: {ArticleUrl}", articleUrl);
        }

        return null;
    }

    public async Task<bool> IsImageCachedAsync(string articleUrl)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return false;

        var cacheData = await _cacheService.GetItemAsync<CachedImageData>(CacheNamespace, articleUrl);
        return cacheData != null;
    }

    public async Task<bool> InvalidateCacheAsync(string articleUrl)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return false;

        try
        {
            await _cacheService.RemoveItemAsync(CacheNamespace, articleUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache for article: {ArticleUrl}", articleUrl);
            return false;
        }
    }

    public async Task<int> ClearCacheAsync()
    {
        try
        {
            await _cacheService.ClearNamespaceAsync(CacheNamespace);
            return 0; // ClearNamespaceAsync returns void, we can't count removed items
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            return 0;
        }
    }

    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        try
        {
            var stats = await _cacheService.GetNamespaceStatsAsync(CacheNamespace);
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
            _logger.LogError(ex, "Failed to get cache statistics");
            return new Dictionary<string, object>();
        }
    }

    public async Task<int> CleanupExpiredEntriesAsync()
    {
        try
        {
            return await _cacheService.CleanupExpiredItemsAsync(CacheNamespace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired entries");
            return 0;
        }
    }

    /// <summary>
    /// Retrieves images from the API for multiple URLs in a single batch request,
    /// then validates them in parallel with proper error handling.
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
                UseCache = false // We're handling cache at service level
            };

            var json = JsonSerializer.Serialize(batchRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Making batch API call for {Count} articles", articleUrls.Count);

            // Call the Azure Function API
            var response = await httpClient.PostAsync("/api/GetOpenGraphImages", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var batchResponse = JsonSerializer.Deserialize<BatchImageResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (batchResponse?.Results?.Any() == true)
                {
                    // Phase 1: Collect successful results for validation
                    var imagesToValidate = new List<(string ArticleUrl, string ImageUrl, ImageSource ImageSource)>();
                    
                    foreach (var articleResult in batchResponse.Results)
                    {
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

                            await SaveImageToCacheAsync(failedCacheData);
                            result[articleResult.ArticleUrl] = null;
                        }
                    }

                    // Phase 2: Validate images in parallel
                    if (imagesToValidate.Any())
                    {
                        _logger.LogInformation("Starting parallel validation of {Count} images", imagesToValidate.Count);
                        
                        var validationResults = await ValidateImagesInParallelAsync(
                            imagesToValidate.Select(x => x.ImageUrl).ToList(), 
                            cancellationToken);

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
                                
                                _logger.LogDebug("Image validation successful for {ArticleUrl}: {ImageUrl}", 
                                    articleUrl, imageUrl);
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
                                
                                _logger.LogWarning("Image validation failed for {ArticleUrl}: {ImageUrl} - {Error}", 
                                    articleUrl, imageUrl, validationResult?.ErrorMessage ?? "Unknown error");
                            }
                        }

                        // Execute batch cache updates for improved performance
                        await SaveImageBatchToCacheAsync(cacheDataBatch);
                        
                        _logger.LogInformation("Completed parallel validation and batch caching for {Count} images", 
                            imagesToValidate.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Batch API response contained no results");
                }
            }
            else
            {
                _logger.LogError("Batch API call failed with status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve images from API for {Count} articles", articleUrls.Count);
        }

        // Ensure all requested URLs have entries in the result, even if they failed
        foreach (var url in articleUrls)
        {
            if (!result.ContainsKey(url))
            {
                result[url] = null;
            }
        }

        return result;
    }

    private async Task SaveImageToCacheAsync(CachedImageData cacheData)
    {
        try
        {
            var expiration = cacheData.ExpiresAt - DateTime.UtcNow;
            if (expiration > TimeSpan.Zero)
            {
                await _cacheService.SetItemAsync(CacheNamespace, cacheData.ArticleUrl, cacheData, (int)expiration.TotalMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save cache data for article: {ArticleUrl}", cacheData.ArticleUrl);
        }
    }

    /// <summary>
    /// Saves multiple images to cache in batch for improved performance.
    /// Uses parallel processing with controlled concurrency to avoid overwhelming the storage system.
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
            _logger.LogDebug("Starting batch cache save for {Count} items", cacheDataBatch.Count);

            // Use controlled concurrency to avoid overwhelming the browser storage
            var maxConcurrency = Math.Min(cacheDataBatch.Count, 10); // Limit concurrent cache operations
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var cacheTasks = cacheDataBatch.Select(async cacheData =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var expiration = cacheData.ExpiresAt - DateTime.UtcNow;
                    if (expiration > TimeSpan.Zero)
                    {
                        await _cacheService.SetItemAsync(CacheNamespace, cacheData.ArticleUrl, cacheData, (int)expiration.TotalMinutes, cancellationToken);
                        _logger.LogTrace("Successfully cached image data for article: {ArticleUrl}", cacheData.ArticleUrl);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping cache save for expired data: {ArticleUrl}", cacheData.ArticleUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save cache data for article: {ArticleUrl}", cacheData.ArticleUrl);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(cacheTasks);

            _logger.LogInformation("Completed batch cache save for {Count} items with max concurrency {MaxConcurrency}", 
                cacheDataBatch.Count, maxConcurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform batch cache save for {Count} items", cacheDataBatch.Count);
        }
    }


    /// <summary>
    /// Validates multiple image URLs in parallel using the image validation service.
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
            
            _logger.LogDebug("Starting parallel image validation for {Count} URLs with max concurrency {MaxConcurrency}", 
                imageUrls.Count, maxConcurrency);
            
            // Call the validation service which handles parallel processing internally
            var validationResults = await _imageValidationService.ValidateImagesAsync(
                imageUrls, 
                maxConcurrency, 
                cancellationToken);
            
            // Process results with comprehensive error handling
            foreach (var imageUrl in imageUrls)
            {
                if (validationResults.TryGetValue(imageUrl, out var validationResult))
                {
                    result[imageUrl] = validationResult;
                    
                    if (validationResult.IsValid)
                    {
                        _logger.LogTrace("Image validation successful: {ImageUrl} (Content-Type: {ContentType}, Size: {Size})", 
                            imageUrl, validationResult.ContentType, validationResult.ContentLength);
                    }
                    else
                    {
                        _logger.LogDebug("Image validation failed: {ImageUrl} - {Error}", 
                            imageUrl, validationResult.ErrorMessage);
                    }
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
                    _logger.LogWarning("No validation result found for image: {ImageUrl}", imageUrl);
                }
            }
            
            var validCount = result.Values.Count(v => v.IsValid);
            var invalidCount = result.Values.Count - validCount;
            
            _logger.LogInformation("Parallel image validation completed: {ValidCount} valid, {InvalidCount} invalid out of {Total} total", 
                validCount, invalidCount, imageUrls.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform parallel image validation for {Count} URLs", imageUrls.Count);
            
            // Fallback: Mark all images as invalid to avoid blocking the process
            foreach (var imageUrl in imageUrls)
            {
                if (!result.ContainsKey(imageUrl))
                {
                    result[imageUrl] = new ImageValidationResult
                    {
                        ImageUrl = imageUrl,
                        IsValid = false,
                        ErrorMessage = $"Validation failed due to system error: {ex.Message}",
                        ValidatedAt = DateTime.UtcNow
                    };
                }
            }
        }
        
        return result;
    }

    public async Task<bool> UpdateCacheEntryAsync(string articleUrl, CachedImageData imageData)
    {
        if (string.IsNullOrWhiteSpace(articleUrl) || imageData == null)
            return false;

        try
        {
            var expiration = imageData.ExpiresAt - DateTime.UtcNow;
            if (expiration > TimeSpan.Zero)
            {
                await _cacheService.SetItemAsync(CacheNamespace, articleUrl, imageData, (int)expiration.TotalMinutes);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cache entry for article: {ArticleUrl}", articleUrl);
            return false;
        }
    }
}
