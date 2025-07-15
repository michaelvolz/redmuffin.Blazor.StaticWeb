using System.ComponentModel.DataAnnotations;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
/// Represents a batch request for image processing for multiple articles.
/// </summary>
public class BatchImageRequest
{
    /// <summary>
    /// Gets or sets the collection of article requests to process.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<ArticleImageRequest> Articles { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests to process.
    /// </summary>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for the entire batch operation.
    /// </summary>
    public int BatchTimeoutMs { get; set; } = 300000; // 5 minutes default

    /// <summary>
    /// Gets or sets the timeout in milliseconds for individual article requests.
    /// </summary>
    public int ArticleTimeoutMs { get; set; } = 30000; // 30 seconds default

    /// <summary>
    /// Gets or sets a value indicating whether to stop processing if any request fails.
    /// </summary>
    public bool StopOnFirstError { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to validate extracted image URLs.
    /// </summary>
    public bool ValidateImages { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of images to extract per article.
    /// </summary>
    public int MaxImagesPerArticle { get; set; } = 5;

    /// <summary>
    /// Gets or sets custom User-Agent string for HTTP requests.
    /// </summary>
    public string UserAgent { get; set; } = "redmuffin-blazor-staticweb/1.0";

    /// <summary>
    /// Gets or sets a value indicating whether to use cached results if available.
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the request identifier for tracking purposes.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the priority of this batch request (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 5;
}
