namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache health metrics for monitoring system performance.
/// </summary>
public class CacheHealthMetrics
{
    /// <summary>
    ///     Overall cache health status.
    /// </summary>
    public CacheHealthStatus HealthStatus { get; set; }

    /// <summary>
    ///     Storage utilization percentage.
    /// </summary>
    public double StorageUtilizationPercent { get; set; }

    /// <summary>
    ///     Number of expired items that need cleanup.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    ///     Memory pressure indicator.
    /// </summary>
    public bool IsMemoryPressureHigh { get; set; }

    /// <summary>
    ///     Cache fragmentation percentage.
    /// </summary>
    public double FragmentationPercent { get; set; }

    /// <summary>
    ///     Performance issues detected.
    /// </summary>
    public List<string> PerformanceIssues { get; set; } = new();

    /// <summary>
    ///     Health check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}