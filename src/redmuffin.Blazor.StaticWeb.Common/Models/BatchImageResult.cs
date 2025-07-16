namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents the result of a batch image retrieval operation
/// </summary>
public class BatchImageResult
{
    /// <summary>
    ///     Gets or sets whether the entire batch operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    ///     Gets or sets the results for each article URL
    /// </summary>
    public IDictionary<string, ImageResult> Results { get; set; } = new Dictionary<string, ImageResult>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets the total processing time in milliseconds
    /// </summary>
    public int TotalProcessingTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the number of successful retrievals
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of failed retrievals
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of cache hits
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    ///     Gets or sets any global error messages
    /// </summary>
    public IList<string> GlobalErrors { get; set; } = [];

    /// <summary>
    ///     Creates a successful batch result
    /// </summary>
    public static BatchImageResult Success(IDictionary<string, ImageResult> results, int totalProcessingTimeMs = 0)
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
            CacheHitCount = cacheHitCount,
        };
    }

    /// <summary>
    ///     Creates a failed batch result
    /// </summary>
    public static BatchImageResult Failure(string errorMessage, int totalProcessingTimeMs = 0)
    {
        return new BatchImageResult
        {
            IsSuccess = false,
            GlobalErrors = new List<string> { errorMessage },
            TotalProcessingTimeMs = totalProcessingTimeMs,
        };
    }
}