namespace redmuffin.Blazor.StaticWeb.Common.Models;

/// <summary>
///     Represents performance metrics for a batch operation.
/// </summary>
public class BatchPerformanceMetrics
{
    /// <summary>
    ///     Gets or sets the average processing time per article in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the maximum processing time for a single article in milliseconds.
    /// </summary>
    public int MaxProcessingTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the minimum processing time for a single article in milliseconds.
    /// </summary>
    public int MinProcessingTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the number of concurrent tasks that were active during processing.
    /// </summary>
    public int ConcurrentTaskCount { get; set; }

    /// <summary>
    ///     Gets or sets the memory usage in bytes during peak processing.
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    ///     Gets or sets the total number of HTTP requests made during processing.
    /// </summary>
    public int TotalHttpRequests { get; set; }

    /// <summary>
    ///     Gets or sets the total number of validation requests made.
    /// </summary>
    public int TotalValidationRequests { get; set; }

    /// <summary>
    ///     Gets or sets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }
}