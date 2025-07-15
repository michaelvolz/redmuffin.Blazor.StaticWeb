namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
/// Represents the response containing batch image processing results for multiple articles.
/// </summary>
public class BatchImageResponse
{
    /// <summary>
    /// Gets or sets the request identifier that was provided in the batch request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the entire batch operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the collection of article image processing results.
    /// </summary>
    public List<ArticleImageResponse> Results { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of articles that were processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of articles that were processed successfully.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of articles that failed processing.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the number of articles that were served from cache.
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    /// Gets or sets the collection of error messages if any occurred during batch processing.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the time it took to process the entire batch in milliseconds.
    /// </summary>
    public int TotalProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the batch processing was completed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the batch processing was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets performance metrics for the batch operation.
    /// </summary>
    public BatchPerformanceMetrics? PerformanceMetrics { get; set; }
}

/// <summary>
/// Represents performance metrics for a batch operation.
/// </summary>
public class BatchPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the average processing time per article in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum processing time for a single article in milliseconds.
    /// </summary>
    public int MaxProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum processing time for a single article in milliseconds.
    /// </summary>
    public int MinProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of concurrent tasks that were active during processing.
    /// </summary>
    public int ConcurrentTaskCount { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes during peak processing.
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the total number of HTTP requests made during processing.
    /// </summary>
    public int TotalHttpRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of validation requests made.
    /// </summary>
    public int TotalValidationRequests { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }
}
