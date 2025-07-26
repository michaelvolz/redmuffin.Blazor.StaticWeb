using System.Security.Cryptography;
using System.Text;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;

/// <summary>
///     Simple image validation service with localStorage caching.
///     Provides a lean, maintainable approach to image validation without complex orchestration.
/// </summary>
public sealed class SimpleImageValidationService : ISimpleImageValidationService
{
    private const string CacheKeyPrefix = "img_validation_";
    private const int DefaultTimeoutMs = 5000; // 5 seconds - shorter than complex service
    private const int CacheExpirationMinutes = 40320; // 4 weeks cache
    private const double CacheCleanupThreshold = 0.75; // Clean up at 75% quota
    private const int TimeoutFailureCacheMinutes = 30; // Cache timeout failures for 30 minutes

    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, string, Exception?> LogImageValidationStarted =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(LogImageValidationStarted)),
            "Starting image validation for URL: {ImageUrl}");

    private static readonly Action<ILogger, string, Exception?> LogImageValidationSuccess =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, nameof(LogImageValidationSuccess)),
            "Image validation successful for URL: {ImageUrl}");

    private static readonly Action<ILogger, string, string, Exception?> LogImageValidationFailed =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(3, nameof(LogImageValidationFailed)),
            "Image validation failed for URL: {ImageUrl}, Reason: {Reason}");

    private static readonly Action<ILogger, string, Exception?> LogCacheHit =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, nameof(LogCacheHit)),
            "Cache hit for image URL: {ImageUrl}");

    private static readonly Action<ILogger, string, Exception?> LogCacheMiss =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, nameof(LogCacheMiss)),
            "Cache miss for image URL: {ImageUrl}");

    private static readonly Action<ILogger, Exception> LogCacheCleanupFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(6, nameof(LogCacheCleanupFailed)),
            "Failed to perform cache cleanup");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBrowserStorageService _browserStorageService;
    private readonly ILogger<SimpleImageValidationService> _logger;

    public SimpleImageValidationService(
        IHttpClientFactory httpClientFactory,
        IBrowserStorageService browserStorageService,
        ILogger<SimpleImageValidationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _browserStorageService = browserStorageService ?? throw new ArgumentNullException(nameof(browserStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Computes SHA256 hash of the image URL for cache key generation.
    /// </summary>
    private static string ComputeUrlHash(string imageUrl)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl));
        return Convert.ToBase64String(bytes)[..16]; // Use first 16 characters for shorter keys
    }

    /// <summary>
    ///     Generates cache key for the image URL.
    /// </summary>
    private static string GetCacheKey(string imageUrl)
    {
        var urlHash = ComputeUrlHash(imageUrl);
        return CacheKeyPrefix + urlHash;
    }

    /// <summary>
    ///     Validates content type to ensure it's an image.
    /// </summary>
    private static bool IsValidImageContentType(string? contentType)
    {
        return contentType != null && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Upgrades HTTP URLs to HTTPS for better security and CORS compliance.
    /// </summary>
    private static string UpgradeToHttpsIfNeeded(string imageUrl)
    {
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return string.Concat("https://", imageUrl.AsSpan(7));

        return imageUrl;
    }

    /// <summary>
    ///     Determines if a failure reason indicates a timeout or network issue.
    /// </summary>
    private static bool IsTimeoutFailure(string? failureReason)
    {
        return failureReason != null &&
               (failureReason.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                failureReason.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
                failureReason.Contains("network", StringComparison.OrdinalIgnoreCase));
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

    public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return ImageValidationResult.Failure("Image URL is null or empty");

        LogImageValidationStarted(_logger, imageUrl, null);

        // Upgrade to HTTPS if needed
        var processedUrl = UpgradeToHttpsIfNeeded(imageUrl);

        // Validate URL format
        if (!Uri.TryCreate(processedUrl, UriKind.Absolute, out var uri)) return ImageValidationResult.Failure("Invalid URL format");

        try
        {
            var result = await PerformHttpValidationAsync(imageUrl, uri, cancellationToken).ConfigureAwait(false);
            await CacheValidationResultAsync(imageUrl, result, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (HttpRequestException ex)
        {
            return await HandleValidationExceptionAsync(imageUrl, $"HTTP request failed: {ex.Message}", ex, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            var reason = ex.CancellationToken == cancellationToken ? "Operation was cancelled" : "Request timed out";
            return await HandleValidationExceptionAsync(imageUrl, reason, ex, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await HandleValidationExceptionAsync(imageUrl, $"Validation failed: {ex.Message}", ex, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImageValidationResult?> GetCachedResultAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;

        try
        {
            var cacheKey = GetCacheKey(imageUrl);
            var cachedResult = await _browserStorageService.GetItemAsync<ImageValidationResult>(cacheKey, cancellationToken).ConfigureAwait(false);

            if (cachedResult != null)
            {
                // Determine cache expiration based on failure reason
                var cacheExpirationMinutes = IsTimeoutFailure(cachedResult.FailureReason)
                    ? TimeoutFailureCacheMinutes
                    : CacheExpirationMinutes;

                // Check if cached result is still valid
                if (DateTime.UtcNow - cachedResult.ValidatedAt < TimeSpan.FromMinutes(cacheExpirationMinutes))
                {
                    LogCacheHit(_logger, imageUrl, null);
                    return cachedResult;
                }

                // Cached result is expired, remove it
                await _browserStorageService.RemoveItemAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            }

            LogCacheMiss(_logger, imageUrl, null);
            return null;
        }
        catch (Exception ex)
        {
            // Log but don't fail - return null to indicate cache miss
            LogCacheMiss(_logger, imageUrl, ex);
            return null;
        }
    }

    public async Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return GenerateSimplePlaceholder("No image URL provided");

        // Check cache first
        var cachedResult = await GetCachedResultAsync(imageUrl, cancellationToken).ConfigureAwait(false);
        if (cachedResult != null) return cachedResult.IsValid ? imageUrl : GenerateSimplePlaceholder(cachedResult.FailureReason ?? "Image not available");

        // Perform validation
        var validationResult = await ValidateImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
        return validationResult.IsValid ? imageUrl : GenerateSimplePlaceholder(validationResult.FailureReason ?? "Image validation failed");
    }

    /// <summary>
    ///     Performs the HTTP HEAD request and validates the response.
    /// </summary>
    private async Task<ImageValidationResult> PerformHttpValidationAsync(string imageUrl, Uri uri, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMilliseconds(DefaultTimeoutMs);

        using var request = new HttpRequestMessage(HttpMethod.Head, uri);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var reason = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            LogImageValidationFailed(_logger, imageUrl, reason, null);
            return ImageValidationResult.Failure(reason);
        }

        // Check content type
        var contentType = response.Content?.Headers?.ContentType?.MediaType;
        if (!IsValidImageContentType(contentType))
        {
            var reason = $"Content type '{contentType}' is not an image";
            LogImageValidationFailed(_logger, imageUrl, reason, null);
            return ImageValidationResult.Failure(reason);
        }

        LogImageValidationSuccess(_logger, imageUrl, null);
        return ImageValidationResult.Success();
    }

    /// <summary>
    ///     Handles validation exceptions by logging and caching the failure.
    /// </summary>
    private async Task<ImageValidationResult> HandleValidationExceptionAsync(string imageUrl, string reason, Exception ex, CancellationToken cancellationToken)
    {
        LogImageValidationFailed(_logger, imageUrl, reason, ex);
        var result = ImageValidationResult.Failure(reason);
        await CacheValidationResultAsync(imageUrl, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    ///     Caches the validation result and performs cleanup if needed.
    /// </summary>
    private async Task CacheValidationResultAsync(string imageUrl, ImageValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = GetCacheKey(imageUrl);
            await _browserStorageService.SetItemAsync(cacheKey, result, cancellationToken).ConfigureAwait(false);

            // Perform cleanup if storage is getting full
            await PerformCacheCleanupIfNeededAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't fail validation on cache errors
            LogCacheCleanupFailed(_logger, ex);
        }
    }

    /// <summary>
    ///     Performs cache cleanup if storage usage exceeds threshold.
    /// </summary>
    private async Task PerformCacheCleanupIfNeededAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);
            var usagePercent = stats.QuotaUsagePercent;

            if (usagePercent > CacheCleanupThreshold * 100)
            {
                // Clean up expired items first
                await _browserStorageService.CleanupExpiredItemsAsync(cancellationToken).ConfigureAwait(false);

                // If still over threshold, perform LRU eviction
                var updatedStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);
                if (updatedStats.QuotaUsagePercent > CacheCleanupThreshold * 100)
                {
                    var targetSize = (long)(updatedStats.QuotaLimitBytes * 0.6); // Target 60% usage
                    await _browserStorageService.EvictLeastRecentlyUsedAsync(targetSize, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            LogCacheCleanupFailed(_logger, ex);
        }
    }
}