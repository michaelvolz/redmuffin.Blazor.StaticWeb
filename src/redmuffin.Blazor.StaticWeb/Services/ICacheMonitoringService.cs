namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
/// Service for comprehensive cache monitoring and performance optimization.
/// Provides system-wide cache statistics and health metrics.
/// </summary>
public interface ICacheMonitoringService
{
    /// <summary>
    /// Gets comprehensive cache statistics across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Comprehensive cache statistics</returns>
    Task<CacheMonitoringStats> GetComprehensiveCacheStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache health metrics including performance indicators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache health metrics</returns>
    Task<CacheHealthMetrics> GetCacheHealthMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache performance statistics over time.
    /// </summary>
    /// <param name="timeRangeHours">Time range in hours for performance analysis</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache performance statistics</returns>
    Task<CachePerformanceStats> GetCachePerformanceStatsAsync(int timeRangeHours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cache optimization by cleaning up expired items and optimizing storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache optimization result</returns>
    Task<CacheOptimizationResult> OptimizeCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache usage recommendations based on current performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache usage recommendations</returns>
    Task<CacheRecommendations> GetCacheRecommendationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Comprehensive cache monitoring statistics.
/// </summary>
public class CacheMonitoringStats
{
    /// <summary>
    /// Overall cache statistics.
    /// </summary>
    public CacheStats Overall { get; set; } = new();

    /// <summary>
    /// OpenGraph image cache statistics.
    /// </summary>
    public Dictionary<string, object> OpenGraphStats { get; set; } = new();

    /// <summary>
    /// Image validation cache statistics.
    /// </summary>
    public Dictionary<string, object> ImageValidationStats { get; set; } = new();

    /// <summary>
    /// Cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Cache miss rate as a percentage.
    /// </summary>
    public double CacheMissRate { get; set; }

    /// <summary>
    /// Average cache access time in milliseconds.
    /// </summary>
    public double AverageAccessTimeMs { get; set; }

    /// <summary>
    /// Total cache operations performed.
    /// </summary>
    public long TotalCacheOperations { get; set; }

    /// <summary>
    /// Statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache health metrics for monitoring system performance.
/// </summary>
public class CacheHealthMetrics
{
    /// <summary>
    /// Overall cache health status.
    /// </summary>
    public CacheHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Storage utilization percentage.
    /// </summary>
    public double StorageUtilizationPercent { get; set; }

    /// <summary>
    /// Number of expired items that need cleanup.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    /// Memory pressure indicator.
    /// </summary>
    public bool IsMemoryPressureHigh { get; set; }

    /// <summary>
    /// Cache fragmentation percentage.
    /// </summary>
    public double FragmentationPercent { get; set; }

    /// <summary>
    /// Performance issues detected.
    /// </summary>
    public List<string> PerformanceIssues { get; set; } = new();

    /// <summary>
    /// Health check timestamp.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache performance statistics over time.
/// </summary>
public class CachePerformanceStats
{
    /// <summary>
    /// Time range for these statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    /// Average cache hit rate over the time period.
    /// </summary>
    public double AverageHitRate { get; set; }

    /// <summary>
    /// Peak storage usage in bytes.
    /// </summary>
    public long PeakStorageUsage { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Total cache operations during the time period.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Number of cache evictions performed.
    /// </summary>
    public int EvictionsCount { get; set; }

    /// <summary>
    /// Number of cache cleanups performed.
    /// </summary>
    public int CleanupCount { get; set; }

    /// <summary>
    /// Statistics collection timestamp.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache optimization result.
/// </summary>
public class CacheOptimizationResult
{
    /// <summary>
    /// Whether optimization was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Number of expired items removed.
    /// </summary>
    public int ExpiredItemsRemoved { get; set; }

    /// <summary>
    /// Number of items evicted for optimization.
    /// </summary>
    public int ItemsEvicted { get; set; }

    /// <summary>
    /// Storage space freed in bytes.
    /// </summary>
    public long StorageFreedBytes { get; set; }

    /// <summary>
    /// Time taken for optimization in milliseconds.
    /// </summary>
    public long OptimizationTimeMs { get; set; }

    /// <summary>
    /// Storage utilization before optimization.
    /// </summary>
    public double StorageUtilizationBefore { get; set; }

    /// <summary>
    /// Storage utilization after optimization.
    /// </summary>
    public double StorageUtilizationAfter { get; set; }

    /// <summary>
    /// Optimization actions performed.
    /// </summary>
    public List<string> ActionsPerformed { get; set; } = new();

    /// <summary>
    /// Optimization timestamp.
    /// </summary>
    public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache usage recommendations.
/// </summary>
public class CacheRecommendations
{
    /// <summary>
    /// Recommended cache size adjustments.
    /// </summary>
    public List<string> SizeRecommendations { get; set; } = new();

    /// <summary>
    /// Recommended expiration policy changes.
    /// </summary>
    public List<string> ExpirationRecommendations { get; set; } = new();

    /// <summary>
    /// Recommended performance optimizations.
    /// </summary>
    public List<string> PerformanceRecommendations { get; set; } = new();

    /// <summary>
    /// Recommended maintenance actions.
    /// </summary>
    public List<string> MaintenanceRecommendations { get; set; } = new();

    /// <summary>
    /// Overall cache health score (0-100).
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Recommendations generated timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache health status enumeration.
/// </summary>
public enum CacheHealthStatus
{
    /// <summary>
    /// Cache is healthy and performing well.
    /// </summary>
    Healthy,

    /// <summary>
    /// Cache has some performance issues but is functional.
    /// </summary>
    Warning,

    /// <summary>
    /// Cache has significant performance issues.
    /// </summary>
    Critical,

    /// <summary>
    /// Cache is experiencing severe problems.
    /// </summary>
    Error
}
