using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
/// Service for validating image URLs using HTTP HEAD requests to ensure they're accessible and valid.
/// </summary>
public class ImageValidationService : IImageValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ImageValidationService> _logger;
    private readonly ConcurrentDictionary<string, ImageValidationResult> _memoryCache = new();
    private readonly SemaphoreSlim _concurrentRequestsSemaphore = new(10, 10);

    private const string CacheNamespace = "image_validation";
    private const int CacheExpirationHours = 1; // 1 hour for image validation results
    private const int DefaultTimeoutMs = 10000; // 10 seconds

    public ImageValidationService(
        IHttpClientFactory httpClientFactory,
        ICacheService cacheService,
        ILogger<ImageValidationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Image URL is null or empty",
                ValidatedAt = DateTime.UtcNow
            };
        }

        // Store original URL for reference
        string originalUrl = imageUrl;
        
        // Change HTTP to HTTPS if possible for better security and CORS compliance
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            imageUrl = "https://" + imageUrl.Substring(7);
            _logger.LogDebug("Upgraded HTTP to HTTPS: {OriginalUrl} → {HttpsUrl}", originalUrl, imageUrl);
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Invalid URL format",
                ValidatedAt = DateTime.UtcNow
            };
        }

        await _concurrentRequestsSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await PerformHttpHeadValidationAsync(imageUrl, uri, cancellationToken);
        }
        finally
        {
            _concurrentRequestsSemaphore.Release();
        }
    }

    public async Task<Dictionary<string, ImageValidationResult>> ValidateImagesAsync(
        IEnumerable<string> imageUrls, 
        int maxConcurrency = 5, 
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentDictionary<string, ImageValidationResult>();
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = imageUrls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await ValidateImageAsync(url, cancellationToken);
                results.TryAdd(url, result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task<ImageValidationResult> ValidateImageWithCacheAsync(
        string imageUrl, 
        int cacheExpirationMinutes = 60, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Image URL is null or empty",
                ValidatedAt = DateTime.UtcNow
            };
        }

        // Check memory cache first
        if (_memoryCache.TryGetValue(imageUrl, out var memoryCachedResult) && 
            memoryCachedResult.ValidatedAt.AddMinutes(cacheExpirationMinutes) > DateTime.UtcNow)
        {
            return memoryCachedResult;
        }

        // Check persistent cache
        var cachedResult = await _cacheService.GetItemAsync<ImageValidationResult>(CacheNamespace, imageUrl, cancellationToken);
        if (cachedResult != null && 
            cachedResult.ValidatedAt.AddMinutes(cacheExpirationMinutes) > DateTime.UtcNow)
        {
            // Update memory cache
            _memoryCache.TryAdd(imageUrl, cachedResult);
            return cachedResult;
        }

        // Perform validation
        var result = await ValidateImageAsync(imageUrl, cancellationToken);
        
        // Cache the result
        await _cacheService.SetItemAsync(CacheNamespace, imageUrl, result, CacheExpirationHours * 60, cancellationToken);
        _memoryCache.TryAdd(imageUrl, result);

        return result;
    }

    public async Task ClearValidationCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear memory cache
            _memoryCache.Clear();

            // Clear persistent cache
            await _cacheService.ClearNamespaceAsync(CacheNamespace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation cache");
        }
    }

    public async Task<Dictionary<string, object>> GetValidationCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _cacheService.GetNamespaceStatsAsync(CacheNamespace);
            return new Dictionary<string, object>
            {
                ["Namespace"] = stats.Namespace,
                ["TotalEntries"] = stats.TotalItems,
                ["TotalSizeBytes"] = stats.TotalSizeBytes,
                ["ExpiredEntries"] = stats.ExpiredItemsCount,
                ["MemoryCacheCount"] = _memoryCache.Count,
                ["OldestItemTimestamp"] = stats.OldestItemTimestamp?.ToString() ?? "N/A",
                ["NewestItemTimestamp"] = stats.NewestItemTimestamp?.ToString() ?? "N/A",
                ["AverageAccessCount"] = stats.AverageAccessCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get validation cache statistics");
            return new Dictionary<string, object>();
        }
    }

    private async Task<ImageValidationResult> PerformHttpHeadValidationAsync(
        string imageUrl, 
        Uri uri, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ExternalHttpClient");
            // Override timeout if needed (ExternalHttpClient has 30s default)
            if (httpClient.Timeout.TotalMilliseconds > DefaultTimeoutMs)
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(DefaultTimeoutMs);
            }

            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            
            using var response = await httpClient.SendAsync(request, cancellationToken);
            
            var result = new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                ContentType = response.Content?.Headers?.ContentType?.MediaType ?? string.Empty,
                ContentLength = response.Content?.Headers?.ContentLength,
                ValidatedAt = DateTime.UtcNow,
                ResponseTimeMs = 0 // Could implement timing if needed
            };

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }
            else if (result.ContentType != null && !result.ContentType.StartsWith("image/"))
            {
                result.IsValid = false;
                result.ErrorMessage = $"Content type '{result.ContentType}' is not an image";
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP request failed for image validation: {ImageUrl}", imageUrl);
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = $"HTTP request failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Image validation timed out: {ImageUrl}", imageUrl);
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Request timed out",
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image validation: {ImageUrl}", imageUrl);
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = $"Validation failed: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

}
