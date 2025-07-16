using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for retrieving Open Graph images from articles and managing their cache.
/// </summary>
public interface IOpenGraphImagesService
{
    /// <summary>
    ///     Retrieves Open Graph image for a single article URL.
    /// </summary>
    /// <param name="articleUrl">The URL of the article to process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cached image data or null if no image found</returns>
    Task<CachedImageData?> GetImageAsync(string articleUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves Open Graph images for multiple article URLs in batch.
    /// </summary>
    /// <param name="articleUrls">Collection of article URLs to process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Dictionary mapping article URLs to their cached image data</returns>
    Task<IDictionary<string, CachedImageData?>> GetImagesAsync(IEnumerable<string> articleUrls, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves an image from cache if available, otherwise fetches from API.
    /// </summary>
    /// <param name="articleUrl">The URL of the article</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cached image data or null if not found</returns>
    Task<CachedImageData?> GetImageFromCacheOrApiAsync(string articleUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an image is cached for the given article URL.
    /// </summary>
    /// <param name="articleUrl">The URL of the article</param>
    /// <returns>True if image is cached and not expired</returns>
    Task<bool> IsImageCachedAsync(string articleUrl);

    /// <summary>
    ///     Invalidates the cache for a specific article URL.
    /// </summary>
    /// <param name="articleUrl">The URL of the article to invalidate</param>
    /// <returns>True if cache entry was found and removed</returns>
    Task<bool> InvalidateCacheAsync(string articleUrl);

    /// <summary>
    ///     Clears all cached images.
    /// </summary>
    /// <returns>Number of cache entries removed</returns>
    Task<int> ClearCacheAsync();

    /// <summary>
    ///     Gets cache statistics for monitoring and optimization.
    /// </summary>
    /// <returns>Dictionary containing cache statistics</returns>
    Task<IDictionary<string, object>> GetCacheStatsAsync();

    /// <summary>
    ///     Updates an existing cache entry with new data.
    /// </summary>
    /// <param name="articleUrl">The article URL to update</param>
    /// <param name="imageData">The updated image data</param>
    /// <returns>True if update was successful, false otherwise</returns>
    Task<bool> UpdateCacheEntryAsync(string articleUrl, CachedImageData imageData);

    /// <summary>
    ///     Performs cache cleanup by removing expired entries.
    /// </summary>
    /// <returns>Number of expired entries removed</returns>
    Task<int> CleanupExpiredEntriesAsync();
}