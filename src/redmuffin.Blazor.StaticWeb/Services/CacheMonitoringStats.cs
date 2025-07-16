namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Comprehensive cache monitoring statistics.
/// </summary>
public class CacheMonitoringStats
{
    /// <summary>
    ///     Overall cache statistics.
    /// </summary>
    public CacheStats Overall { get; set; } = new();

    /// <summary>
    ///     OpenGraph image cache statistics.
    /// </summary>
    public IDictionary<string, object> OpenGraphStats { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Image validation cache statistics.
    /// </summary>
    public IDictionary<string, object> ImageValidationStats { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    ///     Cache miss rate as a percentage.
    /// </summary>
    public double CacheMissRate { get; set; }

    /// <summary>
    ///     Average cache access time in milliseconds.
    /// </summary>
    public double AverageAccessTimeMs { get; set; }

    /// <summary>
    ///     Total cache operations performed.
    /// </summary>
    public long TotalCacheOperations { get; set; }

    /// <summary>
    ///     Statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}