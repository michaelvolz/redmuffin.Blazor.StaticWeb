using System.Net;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents the result of validating an image URL's accessibility and properties.
/// </summary>
public class ImageValidationResult
{
    /// <summary>
    ///     Gets or sets the original image URL that was validated.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the image is accessible and valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP status code returned when accessing the image.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    ///     Gets or sets the content type of the image (e.g., "image/jpeg", "image/png").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the size of the image in bytes, if available.
    /// </summary>
    public long? ContentLength { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the validation was performed.
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the time it took to validate the image in milliseconds.
    /// </summary>
    public int ResponseTimeMs { get; set; }
}