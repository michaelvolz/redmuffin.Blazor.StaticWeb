namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for comprehensive cache monitoring and performance optimization.
///     Provides system-wide cache statistics and health metrics.
/// </summary>
public interface ICacheMonitoringService
{
    /// <summary>
    ///     Gets comprehensive cache statistics across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Comprehensive cache statistics</returns>
    Task<CacheMonitoringStats> GetComprehensiveCacheStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets cache health metrics including performance indicators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache health metrics</returns>
    Task<CacheHealthMetrics> GetCacheHealthMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets cache performance statistics over time.
    /// </summary>
    /// <param name="timeRangeHours">Time range in hours for performance analysis</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache performance statistics</returns>
    Task<CachePerformanceStats> GetCachePerformanceStatsAsync(int timeRangeHours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs cache optimization by cleaning up expired items and optimizing storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache optimization result</returns>
    Task<CacheOptimizationResult> OptimizeCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets cache usage recommendations based on current performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Cache usage recommendations</returns>
    Task<CacheRecommendations> GetCacheRecommendationsAsync(CancellationToken cancellationToken = default);
}