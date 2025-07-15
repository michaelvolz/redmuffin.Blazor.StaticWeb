using redmuffin.Blazor.StaticWeb.Common.Enums;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
/// Represents cached image data with metadata and expiration information.
/// </summary>
public class CachedImageData
{
    /// <summary>
    /// Gets or sets the original article URL for which the image was retrieved.
    /// </summary>
    public string ArticleUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the extracted image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the image (Open Graph, Twitter, etc.).
    /// </summary>
    public ImageSource ImageSource { get; set; } = ImageSource.None;

    /// <summary>
    /// Gets or sets a value indicating whether the image URL has been validated as accessible.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Gets or sets the validation result if the image has been validated.
    /// </summary>
    public ImageValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this data was cached.
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this cache entry expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>
    /// Gets or sets the timestamp when this cache entry was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the access count for LRU cache management.
    /// </summary>
    public int AccessCount { get; set; } = 1;

    /// <summary>
    /// Gets a value indicating whether this cache entry has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
