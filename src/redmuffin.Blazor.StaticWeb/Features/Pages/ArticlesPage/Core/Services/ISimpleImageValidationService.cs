using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;

/// <summary>
///     Simple image validation service for verifying image URLs and managing cache.
///     Provides a lean, maintainable approach to image validation with localStorage caching.
/// </summary>
public interface ISimpleImageValidationService
{
    /// <summary>
    ///     Validates an image URL by performing HTTP HEAD request and checking response.
    ///     Results are automatically cached in localStorage for performance.
    /// </summary>
    /// <param name="imageUrl">The image URL to validate</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Validation result with details about the image accessibility</returns>
    Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves cached validation result for an image URL without performing network requests.
    ///     Returns null if no cached result exists or if the cached result has expired.
    /// </summary>
    /// <param name="imageUrl">The image URL to check cache for</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cached validation result or null if not cached/expired</returns>
    Task<ImageValidationResult?> GetCachedResultAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the best available image URL or a placeholder if validation fails.
    ///     This method checks cache first, then validates if needed, and returns either
    ///     the original URL (if valid) or a dynamically generated placeholder.
    /// </summary>
    /// <param name="imageUrl">The image URL to process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Valid image URL or base64-encoded placeholder</returns>
    Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl, CancellationToken cancellationToken = default);
}