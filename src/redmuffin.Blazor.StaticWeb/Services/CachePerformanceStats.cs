namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache performance statistics over time.
/// </summary>
public class CachePerformanceStats
{
    /// <summary>
    ///     Gets or sets time range for these statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    ///     Gets or sets average cache hit rate over the time period.
    /// </summary>
    public double AverageHitRate { get; set; }

    /// <summary>
    ///     Gets or sets peak storage usage in bytes.
    /// </summary>
    public long PeakStorageUsage { get; set; }

    /// <summary>
    ///     Gets or sets average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets total cache operations during the time period.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    ///     Gets or sets number of cache evictions performed.
    /// </summary>
    public int EvictionsCount { get; set; }

    /// <summary>
    ///     Gets or sets number of cache cleanups performed.
    /// </summary>
    public int CleanupCount { get; set; }

    /// <summary>
    ///     Gets or sets statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}