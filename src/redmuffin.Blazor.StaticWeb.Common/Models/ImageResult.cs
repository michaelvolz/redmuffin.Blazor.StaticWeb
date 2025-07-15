using redmuffin.Blazor.StaticWeb.Common.Enums;

namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
/// Represents the result of an image retrieval operation
/// </summary>
public class ImageResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the retrieved image data (null if not successful)
    /// </summary>
    public CachedImageData? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error type for programmatic handling
    /// </summary>
    public ImageRetrievalErrorType ErrorType { get; set; }

    /// <summary>
    /// Gets or sets whether the data came from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Gets or sets the processing time in milliseconds
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// Creates a successful result
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
    /// Creates a failed result
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
    /// Creates a result for when no image was found (not an error)
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

/// <summary>
/// Represents the result of a batch image retrieval operation
/// </summary>
public class BatchImageResult
{
    /// <summary>
    /// Gets or sets whether the entire batch operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the results for each article URL
    /// </summary>
    public Dictionary<string, ImageResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the total processing time in milliseconds
    /// </summary>
    public int TotalProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of successful retrievals
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed retrievals
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    /// Gets or sets any global error messages
    /// </summary>
    public List<string> GlobalErrors { get; set; } = new();

    /// <summary>
    /// Creates a successful batch result
    /// </summary>
    public static BatchImageResult Success(Dictionary<string, ImageResult> results, int totalProcessingTimeMs = 0)
    {
        var successCount = results.Values.Count(r => r.IsSuccess && r.Data != null);
        var failureCount = results.Values.Count(r => !r.IsSuccess);
        var cacheHitCount = results.Values.Count(r => r.IsSuccess && r.FromCache);

        return new BatchImageResult
        {
            IsSuccess = true,
            Results = results,
            TotalProcessingTimeMs = totalProcessingTimeMs,
            SuccessCount = successCount,
            FailureCount = failureCount,
            CacheHitCount = cacheHitCount
        };
    }

    /// <summary>
    /// Creates a failed batch result
    /// </summary>
    public static BatchImageResult Failure(string errorMessage, int totalProcessingTimeMs = 0)
    {
        return new BatchImageResult
        {
            IsSuccess = false,
            GlobalErrors = new List<string> { errorMessage },
            TotalProcessingTimeMs = totalProcessingTimeMs
        };
    }
}
