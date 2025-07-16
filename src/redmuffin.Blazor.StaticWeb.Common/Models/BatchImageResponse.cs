namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents the response containing batch image processing results for multiple articles.
/// </summary>
public class BatchImageResponse
{
    /// <summary>
    ///     Gets or sets the request identifier that was provided in the batch request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the entire batch operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    ///     Gets or sets the collection of article image processing results.
    /// </summary>
    public IList<ArticleImageResponse> Results { get; set; } = [];

    /// <summary>
    ///     Gets or sets the total number of articles that were processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    ///     Gets or sets the number of articles that were processed successfully.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of articles that failed processing.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of articles that were served from cache.
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    ///     Gets or sets the collection of error messages if any occurred during batch processing.
    /// </summary>
    public IList<string> ErrorMessages { get; set; } = [];

    /// <summary>
    ///     Gets or sets the time it took to process the entire batch in milliseconds.
    /// </summary>
    public int TotalProcessingTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the batch processing was completed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the timestamp when the batch processing was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets performance metrics for the batch operation.
    /// </summary>
    public BatchPerformanceMetrics? PerformanceMetrics { get; set; }
}