using System.Collections.Concurrent;
using System.Globalization;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for validating image URLs using HTTP HEAD requests to ensure they're accessible and valid.
/// </summary>
public class ImageValidationService : IImageValidationService, IDisposable
{
    private const string CacheNamespace = "image_validation";
    private const int CacheExpirationHours = 1; // 1 hour for image validation results
    private const int DefaultTimeoutMs = 10000; // 10 seconds

    // LoggerMessage delegates
    private static readonly Action<ILogger, string, string, Exception?> LogUpgradedHttpToHttps =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, nameof(LogUpgradedHttpToHttps)),
            "Upgraded HTTP to HTTPS: {OriginalUrl} → {HttpsUrl}");

    private static readonly Action<ILogger, Exception> LogFailedToClearValidationCache =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogFailedToClearValidationCache)),
            "Failed to clear validation cache");

    private static readonly Action<ILogger, Exception> LogFailedToGetValidationCacheStats =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogFailedToGetValidationCacheStats)),
            "Failed to get validation cache statistics");

    private static readonly Action<ILogger, string, Exception> LogHttpRequestFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, nameof(LogHttpRequestFailed)),
            "HTTP request failed for image validation: {ImageUrl}");

    private static readonly Action<ILogger, string, Exception> LogImageValidationTimedOut =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5, nameof(LogImageValidationTimedOut)),
            "Image validation timed out: {ImageUrl}");

    private static readonly Action<ILogger, string, Exception> LogUnexpectedErrorDuringValidation =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(LogUnexpectedErrorDuringValidation)),
            "Unexpected error during image validation: {ImageUrl}");

    private readonly ICacheService _cacheService;
    private readonly SemaphoreSlim _concurrentRequestsSemaphore = new(10, 10);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageValidationService> _logger;
    private readonly ConcurrentDictionary<string, ImageValidationResult> _memoryCache = new(StringComparer.Ordinal);

    public ImageValidationService(
        IHttpClientFactory httpClientFactory,
        ICacheService cacheService,
        ILogger<ImageValidationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        _concurrentRequestsSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Image URL is null or empty",
                ValidatedAt = DateTime.UtcNow
            };

        // Store original URL for reference
        var originalUrl = imageUrl;

        // Change HTTP to HTTPS if possible for better security and CORS compliance
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            imageUrl = string.Concat("https://", imageUrl.AsSpan(7));
            LogUpgradedHttpToHttps(_logger, originalUrl, imageUrl, null);
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Invalid URL format",
                ValidatedAt = DateTime.UtcNow
            };

        await _concurrentRequestsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await PerformHttpHeadValidationAsync(imageUrl, uri, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _concurrentRequestsSemaphore.Release();
        }
    }

    public async Task<IDictionary<string, ImageValidationResult>> ValidateImagesAsync(
        IEnumerable<string> imageUrls,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentDictionary<string, ImageValidationResult>(StringComparer.Ordinal);
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = imageUrls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await ValidateImageAsync(url, cancellationToken).ConfigureAwait(false);
                results.TryAdd(url, result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
    }

    public async Task<ImageValidationResult> ValidateImageWithCacheAsync(
        string imageUrl,
        int cacheExpirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return new ImageValidationResult
            {
                ImageUrl = imageUrl,
                IsValid = false,
                ErrorMessage = "Image URL is null or empty",
                ValidatedAt = DateTime.UtcNow
            };

        // Check memory cache first
        if (_memoryCache.TryGetValue(imageUrl, out var memoryCachedResult) &&
            memoryCachedResult.ValidatedAt.AddMinutes(cacheExpirationMinutes) > DateTime.UtcNow)
            return memoryCachedResult;

        // Check persistent cache
        var cachedResult = await _cacheService.GetItemAsync<ImageValidationResult>(CacheNamespace, imageUrl, cancellationToken).ConfigureAwait(false);
        if (cachedResult != null &&
            cachedResult.ValidatedAt.AddMinutes(cacheExpirationMinutes) > DateTime.UtcNow)
        {
            // Update memory cache
            _memoryCache.TryAdd(imageUrl, cachedResult);
            return cachedResult;
        }

        // Perform validation
        var result = await ValidateImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);

        // Cache the result
        await _cacheService.SetItemAsync(CacheNamespace, imageUrl, result, CacheExpirationHours * 60, cancellationToken).ConfigureAwait(false);
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
            await _cacheService.ClearNamespaceAsync(CacheNamespace, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogFailedToClearValidationCache(_logger, ex);
        }
    }

    public async Task<IDictionary<string, object>> GetValidationCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _cacheService.GetNamespaceStatsAsync(CacheNamespace, cancellationToken).ConfigureAwait(false);
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Namespace"] = stats.Namespace,
                ["TotalEntries"] = stats.TotalItems,
                ["TotalSizeBytes"] = stats.TotalSizeBytes,
                ["ExpiredEntries"] = stats.ExpiredItemsCount,
                ["MemoryCacheCount"] = _memoryCache.Count,
                ["OldestItemTimestamp"] = stats.OldestItemTimestamp?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                ["NewestItemTimestamp"] = stats.NewestItemTimestamp?.ToString(CultureInfo.InvariantCulture) ?? "N/A",
                ["AverageAccessCount"] = stats.AverageAccessCount
            };
        }
        catch (Exception ex)
        {
            LogFailedToGetValidationCacheStats(_logger, ex);
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
    }

    private async Task<ImageValidationResult> PerformHttpHeadValidationAsync(
        string imageUrl,
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendHttpHeadRequestAsync(uri, cancellationToken).ConfigureAwait(false);
            return CreateValidationResult(response, imageUrl);
        }
        catch (HttpRequestException ex)
        {
            LogHttpRequestFailed(_logger, imageUrl, ex);
            return CreateFailedValidationResult(imageUrl, $"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            LogImageValidationTimedOut(_logger, imageUrl, ex);
            return CreateFailedValidationResult(imageUrl, "Request timed out");
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorDuringValidation(_logger, imageUrl, ex);
            return CreateFailedValidationResult(imageUrl, $"Validation failed: {ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> SendHttpHeadRequestAsync(Uri uri, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("ExternalHttpClient");
        // Override timeout if needed (ExternalHttpClient has 30s default)
        if (httpClient.Timeout.TotalMilliseconds > DefaultTimeoutMs) httpClient.Timeout = TimeSpan.FromMilliseconds(DefaultTimeoutMs);

        using var request = new HttpRequestMessage(HttpMethod.Head, uri);
        return await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static ImageValidationResult CreateValidationResult(HttpResponseMessage response, string imageUrl)
    {
        var result = new ImageValidationResult
        {
            ImageUrl = imageUrl,
            IsValid = response.IsSuccessStatusCode,
            StatusCode = response.StatusCode,
            ContentType = response.Content?.Headers?.ContentType?.MediaType ?? string.Empty,
            ContentLength = response.Content?.Headers?.ContentLength,
            ValidatedAt = DateTime.UtcNow,
            ResponseTimeMs = 0
        };

        if (!response.IsSuccessStatusCode)
        {
            result.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
        }
        else if (!IsValidContentType(result.ContentType))
        {
            result.IsValid = false;
            result.ErrorMessage = $"Content type '{result.ContentType}' is not an image";
        }

        return result;
    }

    private static ImageValidationResult CreateFailedValidationResult(string imageUrl, string errorMessage)
    {
        return new ImageValidationResult
        {
            ImageUrl = imageUrl,
            IsValid = false,
            ErrorMessage = errorMessage,
            ValidatedAt = DateTime.UtcNow
        };
    }

    private static bool IsValidContentType(string? contentType)
    {
        return contentType != null && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}