using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
/// Service for validating image URLs using HTTP HEAD requests to ensure they're accessible and valid.
/// </summary>
public interface IImageValidationService
{
    /// <summary>
    /// Validates a single image URL using HTTP HEAD request.
    /// </summary>
    /// <param name="imageUrl">The image URL to validate</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Validation result containing success status and metadata</returns>
    Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple image URLs in parallel using HTTP HEAD requests.
    /// </summary>
    /// <param name="imageUrls">Collection of image URLs to validate</param>
    /// <param name="maxConcurrency">Maximum number of concurrent validation requests</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Dictionary mapping image URLs to their validation results</returns>
    Task<Dictionary<string, ImageValidationResult>> ValidateImagesAsync(
        IEnumerable<string> imageUrls, 
        int maxConcurrency = 5, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an image URL with caching to avoid redundant requests.
    /// </summary>
    /// <param name="imageUrl">The image URL to validate</param>
    /// <param name="cacheExpirationMinutes">Cache expiration time in minutes (default: 60)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Validation result, potentially from cache</returns>
    Task<ImageValidationResult> ValidateImageWithCacheAsync(
        string imageUrl, 
        int cacheExpirationMinutes = 60, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the validation cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    Task ClearValidationCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets validation cache statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Dictionary containing cache statistics</returns>
    Task<Dictionary<string, object>> GetValidationCacheStatsAsync(CancellationToken cancellationToken = default);
}
