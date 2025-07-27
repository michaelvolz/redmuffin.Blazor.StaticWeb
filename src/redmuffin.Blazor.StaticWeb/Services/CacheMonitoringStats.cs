namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Comprehensive cache monitoring statistics.
/// </summary>
public class CacheMonitoringStats
{
    /// <summary>
    ///     Gets or sets the overall cache statistics.
    /// </summary>
    public CacheStats Overall { get; set; } = new();

    /// <summary>
    ///     Gets or sets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    ///     Gets or sets the cache miss rate as a percentage.
    /// </summary>
    public double CacheMissRate { get; set; }

    /// <summary>
    ///     Gets or sets the average cache access time in milliseconds.
    /// </summary>
    public double AverageAccessTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets the total cache operations performed.
    /// </summary>
    public long TotalCacheOperations { get; set; }

    /// <summary>
    ///     Gets or sets the statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}