using redmuffin.Blazor.StaticWeb.Common.Enums;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
/// Represents the response containing image processing results for a single article.
/// </summary>
public class ArticleImageResponse
{
    /// <summary>
    /// Gets or sets the original article URL that was processed.
    /// </summary>
    public string ArticleUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the image extraction was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the primary image URL extracted from the article.
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the source of the primary image.
    /// </summary>
    public ImageSource PrimaryImageSource { get; set; } = ImageSource.None;

    /// <summary>
    /// Gets or sets the collection of all extracted image URLs with their sources.
    /// </summary>
    public List<ExtractedImage> ExtractedImages { get; set; } = [];

    /// <summary>
    /// Gets or sets the error message if extraction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the time it took to process the article in milliseconds.
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the processing was completed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the result came from cache.
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Gets or sets the cache expiration time if the result was cached.
    /// </summary>
    public DateTime? CacheExpiresAt { get; set; }
}

/// <summary>
/// Represents an extracted image with its source and validation status.
/// </summary>
public class ExtractedImage
{
    /// <summary>
    /// Gets or sets the URL of the extracted image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the image.
    /// </summary>
    public ImageSource Source { get; set; } = ImageSource.None;

    /// <summary>
    /// Gets or sets a value indicating whether the image URL has been validated.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Gets or sets the validation result if the image has been validated.
    /// </summary>
    public ImageValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets the priority of this image (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the image (alt text, dimensions, etc.).
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
