namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache performance statistics over time.
/// </summary>
public class CachePerformanceStats
{
    /// <summary>
    ///     Time range for these statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    ///     Average cache hit rate over the time period.
    /// </summary>
    public double AverageHitRate { get; set; }

    /// <summary>
    ///     Peak storage usage in bytes.
    /// </summary>
    public long PeakStorageUsage { get; set; }

    /// <summary>
    ///     Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    ///     Total cache operations during the time period.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    ///     Number of cache evictions performed.
    /// </summary>
    public int EvictionsCount { get; set; }

    /// <summary>
    ///     Number of cache cleanups performed.
    /// </summary>
    public int CleanupCount { get; set; }

    /// <summary>
    ///     Statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}