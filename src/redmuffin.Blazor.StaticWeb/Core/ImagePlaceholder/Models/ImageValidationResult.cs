namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

/// <summary>
///     Represents the result of an image validation operation.
/// </summary>
public sealed class ImageValidationResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ImageValidationResult" /> class.
    /// </summary>
    /// <param name="isValid">Whether the image is valid</param>
    /// <param name="failureReason">The reason for validation failure, if any</param>
    public ImageValidationResult(bool isValid, string? failureReason = null)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }

    /// <summary>
    ///     Gets a value indicating whether the image is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     Gets the reason for validation failure, if any.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result</returns>
    public static ImageValidationResult Success()
    {
        return new ImageValidationResult(true);
    }

    /// <summary>
    ///     Creates a failed validation result with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for validation failure</param>
    /// <returns>A failed validation result</returns>
    public static ImageValidationResult Failure(string reason)
    {
        return new ImageValidationResult(false, reason);
    }
}