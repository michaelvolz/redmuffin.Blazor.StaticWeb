using System.ComponentModel.DataAnnotations;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents a request for image processing for a single article.
/// </summary>
public class ArticleImageRequest
{
    /// <summary>
    ///     Gets or sets the URL of the article for which to retrieve the image.
    /// </summary>
    [Required]
    [Url]
    public string ArticleUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the article title for context and fallback purposes.
    /// </summary>
    public string ArticleTitle { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the article description for context purposes.
    /// </summary>
    public string ArticleDescription { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the timeout in milliseconds for the HTTP request.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000; // 30 seconds default

    /// <summary>
    ///     Gets or sets a value indicating whether to validate the extracted image URLs.
    /// </summary>
    public bool ValidateImages { get; set; } = true;

    /// <summary>
    ///     Gets or sets the maximum number of images to extract per article.
    /// </summary>
    public int MaxImages { get; set; } = 5;

    /// <summary>
    ///     Gets or sets custom User-Agent string for the HTTP request.
    /// </summary>
    public string UserAgent { get; set; } = "redmuffin-blazor-staticweb/1.0";

    /// <summary>
    ///     Gets or sets a value indicating whether to use cached results if available.
    /// </summary>
    public bool UseCache { get; set; } = true;
}