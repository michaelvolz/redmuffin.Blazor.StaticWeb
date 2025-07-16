using redmuffin.Blazor.StaticWeb.Common.Enums;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents the result of an image retrieval operation
/// </summary>
public class ImageResult
{
    /// <summary>
    ///     Gets or sets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    ///     Gets or sets the retrieved image data (null if not successful)
    /// </summary>
    public CachedImageData? Data { get; set; }

    /// <summary>
    ///     Gets or sets the error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the error type for programmatic handling
    /// </summary>
    public ImageRetrievalErrorType ErrorType { get; set; }

    /// <summary>
    ///     Gets or sets whether the data came from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    ///     Gets or sets the processing time in milliseconds
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    ///     Creates a successful result
    /// </summary>
    public static ImageResult Success(CachedImageData data, bool fromCache = false, int processingTimeMs = 0)
    {
        return new ImageResult
        {
            IsSuccess = true,
            Data = data,
            FromCache = fromCache,
            ProcessingTimeMs = processingTimeMs,
            ErrorType = ImageRetrievalErrorType.None
        };
    }

    /// <summary>
    ///     Creates a failed result
    /// </summary>
    public static ImageResult Failure(string errorMessage, ImageRetrievalErrorType errorType, int processingTimeMs = 0)
    {
        return new ImageResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorType = errorType,
            ProcessingTimeMs = processingTimeMs
        };
    }

    /// <summary>
    ///     Creates a result for when no image was found (not an error)
    /// </summary>
    public static ImageResult NotFound(string reason = "No image found", int processingTimeMs = 0)
    {
        return new ImageResult
        {
            IsSuccess = true, // Not finding an image is not an error
            Data = null,
            ErrorMessage = reason,
            ErrorType = ImageRetrievalErrorType.None,
            ProcessingTimeMs = processingTimeMs
        };
    }
}