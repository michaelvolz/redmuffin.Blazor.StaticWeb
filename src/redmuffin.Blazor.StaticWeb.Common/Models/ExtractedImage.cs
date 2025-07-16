using redmuffin.Blazor.StaticWeb.Common.Enums;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents an extracted image with its source and validation status.
/// </summary>
public class ExtractedImage
{
    /// <summary>
    ///     Gets or sets the URL of the extracted image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the source of the image.
    /// </summary>
    public ImageSource Source { get; set; } = ImageSource.None;

    /// <summary>
    ///     Gets or sets a value indicating whether the image URL has been validated.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    ///     Gets or sets the validation result if the image has been validated.
    /// </summary>
    public ImageValidationResult? ValidationResult { get; set; }

    /// <summary>
    ///     Gets or sets the priority of this image (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata about the image (alt text, dimensions, etc.).
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}