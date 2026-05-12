namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache health metrics for monitoring system performance.
/// </summary>
public class CacheHealthMetrics
{
    /// <summary>
    ///     Gets or sets overall cache health status.
    /// </summary>
    public CacheHealthStatus HealthStatus { get; set; }

    /// <summary>
    ///     Gets or sets storage utilization percentage.
    /// </summary>
    public double StorageUtilizationPercent { get; set; }

    /// <summary>
    ///     Gets or sets number of expired items that need cleanup.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether memory pressure indicator.
    /// </summary>
    public bool IsMemoryPressureHigh { get; set; }

    /// <summary>
    ///     Gets or sets cache fragmentation percentage.
    /// </summary>
    public double FragmentationPercent { get; set; }

    /// <summary>
    ///     Gets or sets performance issues detected.
    /// </summary>
    public IReadOnlyList<string> PerformanceIssues { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets health check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}