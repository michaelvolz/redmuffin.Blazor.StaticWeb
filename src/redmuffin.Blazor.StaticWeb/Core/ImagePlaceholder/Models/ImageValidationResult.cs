namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

/// <summary>
///     Represents the result of an image validation operation.
///     Contains information about whether the image is accessible and any failure details.
/// </summary>
public sealed class ImageValidationResult
{
    /// <summary>
    ///     Gets or sets a value indicating whether the image is valid and accessible.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     Gets or sets the reason for validation failure.
    ///     This property contains details about why the image failed validation,
    ///     such as "CORS blocked", "Image not found", "Invalid format", or "Network error".
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the validation was performed.
    ///     Used for cache expiration and determining when to revalidate images.
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success</returns>
    public static ImageValidationResult Success()
    {
        return new ImageValidationResult
        {
            IsValid = true,
            ValidatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Creates a failed validation result with the specified reason.
    /// </summary>
    /// <param name="failureReason">The reason for validation failure</param>
    /// <returns>A validation result indicating failure</returns>
    public static ImageValidationResult Failure(string failureReason)
    {
        return new ImageValidationResult
        {
            IsValid = false,
            FailureReason = failureReason,
            ValidatedAt = DateTime.UtcNow
        };
    }
}
