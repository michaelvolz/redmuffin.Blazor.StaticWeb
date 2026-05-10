using System.Diagnostics;
using System.Globalization;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Implementation of ICacheMonitoringService for comprehensive cache monitoring and performance optimization.
/// </summary>
public class CacheMonitoringService : ICacheMonitoringService
{
    // Performance thresholds
    private const double HighStorageUtilizationThreshold = 80.0;
    private const double CriticalStorageUtilizationThreshold = 90.0;
    private const double HighFragmentationThreshold = 30.0;

    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Exception?> LogCollectingComprehensiveStats =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogCollectingComprehensiveStats)),
            "Collecting comprehensive cache statistics");

    private static readonly Action<ILogger, Exception?> LogComprehensiveStatsCollected =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogComprehensiveStatsCollected)),
            "Comprehensive cache statistics collected successfully");

    private static readonly Action<ILogger, Exception> LogFailedToCollectStats =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogFailedToCollectStats)),
            "Failed to collect comprehensive cache statistics");

    private static readonly Action<ILogger, Exception?> LogCollectingHealthMetrics =
        LoggerMessage.Define(LogLevel.Debug, new EventId(4, nameof(LogCollectingHealthMetrics)),
            "Collecting cache health metrics");

    private static readonly Action<ILogger, Exception> LogFailedToCollectHealthMetrics =
        LoggerMessage.Define(LogLevel.Error, new EventId(5, nameof(LogFailedToCollectHealthMetrics)),
            "Failed to collect cache health metrics");

    private static readonly Action<ILogger, int, Exception?> LogCollectingPerformanceStats =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(6, nameof(LogCollectingPerformanceStats)),
            "Collecting cache performance statistics for {TimeRange} hours");

    private static readonly Action<ILogger, int, Exception?> LogPerformanceStatsCollected =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(7, nameof(LogPerformanceStatsCollected)),
            "Cache performance statistics collected for {TimeRange} hours");

    private static readonly Action<ILogger, Exception> LogFailedToCollectPerformanceStats =
        LoggerMessage.Define(LogLevel.Error, new EventId(8, nameof(LogFailedToCollectPerformanceStats)),
            "Failed to collect cache performance statistics");

    private static readonly Action<ILogger, Exception?> LogStartingCacheOptimization =
        LoggerMessage.Define(LogLevel.Information, new EventId(9, nameof(LogStartingCacheOptimization)),
            "Starting cache optimization");

    private static readonly Action<ILogger, Exception> LogCacheOptimizationFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(10, nameof(LogCacheOptimizationFailed)),
            "Cache optimization failed");

    private static readonly Action<ILogger, Exception?> LogGeneratingCacheRecommendations =
        LoggerMessage.Define(LogLevel.Debug, new EventId(11, nameof(LogGeneratingCacheRecommendations)),
            "Generating cache recommendations");

    private static readonly Action<ILogger, int, Exception?> LogCacheRecommendationsGenerated =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(12, nameof(LogCacheRecommendationsGenerated)),
            "Cache recommendations generated with health score: {HealthScore}");

    private static readonly Action<ILogger, Exception> LogFailedToGenerateRecommendations =
        LoggerMessage.Define(LogLevel.Error, new EventId(13, nameof(LogFailedToGenerateRecommendations)),
            "Failed to generate cache recommendations");

    private static readonly Action<ILogger, CacheHealthStatus, double, Exception?> LogCacheHealthMetricsCollected =
        LoggerMessage.Define<CacheHealthStatus, double>(LogLevel.Information, new EventId(14, nameof(LogCacheHealthMetricsCollected)),
            "Cache health metrics collected: Status={HealthStatus}, StorageUtilization={StorageUtilization:F2}%");

    private static readonly Action<ILogger, long, double, double, Exception?> LogCacheOptimizationCompleted =
        LoggerMessage.Define<long, double, double>(LogLevel.Information, new EventId(15, nameof(LogCacheOptimizationCompleted)),
            "Cache optimization completed successfully in {ElapsedMs}ms. Storage utilization: {Before:F2}% → {After:F2}%");

    private static readonly Action<ILogger, long, Exception> LogCacheOptimizationFailedWithTime =
        LoggerMessage.Define<long>(LogLevel.Error, new EventId(16, nameof(LogCacheOptimizationFailedWithTime)),
            "Cache optimization failed after {ElapsedMs}ms");

    // Commented out as these are not currently used
    // private const double LowCacheHitRateThreshold = 60.0;
    // private const double CriticalCacheHitRateThreshold = 40.0;
    private readonly IBrowserStorageService _browserStorageService;
    private readonly ILogger<CacheMonitoringService> _logger;

    public CacheMonitoringService(
        IBrowserStorageService browserStorageService,
        ILogger<CacheMonitoringService> logger)
    {
        _browserStorageService = browserStorageService ?? throw new ArgumentNullException(nameof(browserStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static long CalculateTotalAccesses(CacheStats stats)
    {
        // Estimate total accesses based on namespace statistics
        return stats.NamespaceStats.Values.Sum(ns => (long)(ns.TotalItems * ns.AverageAccessCount));
    }

    private static (double HitRate, double MissRate) CalculateHitMissRates(CacheStats stats, long totalAccesses)
    {
        if (totalAccesses == 0) return (0.0, 0.0);

        // Estimate hit rate based on cache efficiency
        var hitRate = Math.Min(95.0, 60.0 + stats.TotalItems * 0.01);
        var missRate = 100.0 - hitRate;

        return (hitRate, missRate);
    }

    private static double CalculateAverageAccessTime(CacheStats stats)
    {
        // Estimate average access time based on storage utilization
        var baseTime = 1.0; // Base access time in milliseconds
        var utilizationFactor = stats.QuotaUsagePercent / 100.0;
        return baseTime * (1.0 + utilizationFactor);
    }

    public static CacheHealthStatus DetermineHealthStatus(double storageUtilization, CacheStats stats)
    {
        if (storageUtilization > CriticalStorageUtilizationThreshold)
            return CacheHealthStatus.Critical;

        if (storageUtilization > HighStorageUtilizationThreshold)
            return CacheHealthStatus.Warning;

        if (stats.TotalExpiredItemsCount > stats.TotalItems * 0.1)
            return CacheHealthStatus.Warning;

        return CacheHealthStatus.Healthy;
    }

    public static List<string> AnalyzePerformanceIssues(CacheStats stats, double storageUtilization)
    {
        var issues = new List<string>();

        if (storageUtilization > CriticalStorageUtilizationThreshold)
            issues.Add($"Critical storage utilization: {storageUtilization.ToString("F2", CultureInfo.InvariantCulture)}%");
        else if (storageUtilization > HighStorageUtilizationThreshold)
            issues.Add($"High storage utilization: {storageUtilization.ToString("F2", CultureInfo.InvariantCulture)}%");

        if (stats.TotalExpiredItemsCount > 0)
            issues.Add($"{stats.TotalExpiredItemsCount.ToString(CultureInfo.InvariantCulture)} expired items need cleanup");

        var fragmentationPercent = CalculateFragmentationPercent(stats);
        if (fragmentationPercent > HighFragmentationThreshold)
            issues.Add($"High cache fragmentation: {fragmentationPercent.ToString("F2", CultureInfo.InvariantCulture)}%");

        return issues;
    }

    private static double CalculateFragmentationPercent(CacheStats stats)
    {
        if (stats.TotalItems == 0) return 0.0;

        // Estimate fragmentation based on expired items ratio
        var expiredRatio = (double)stats.TotalExpiredItemsCount / stats.TotalItems;
        return Math.Min(100.0, expiredRatio * 100.0);
    }

    private static double EstimateAverageHitRate(CacheStats stats)
    {
        // Estimate based on cache efficiency
        return Math.Min(95.0, 60.0 + stats.TotalItems * 0.01);
    }

    private static double EstimateAverageResponseTime(CacheStats stats)
    {
        return CalculateAverageAccessTime(stats);
    }

    private static long EstimateTotalOperations(CacheStats stats, int timeRangeHours)
    {
        // Estimate based on current activity
        var operationsPerHour = stats.TotalItems * 10; // Rough estimate
        return operationsPerHour * timeRangeHours;
    }

    private static int EstimateEvictionsCount(CacheStats stats)
    {
        // Estimate based on storage pressure
        return stats.QuotaUsagePercent > HighStorageUtilizationThreshold ? (int)(stats.TotalItems * 0.1) : 0;
    }

    private static int EstimateCleanupCount(CacheStats stats)
    {
        // Estimate based on expired items
        return stats.TotalExpiredItemsCount > 0 ? 1 : 0;
    }

    public static void AnalyzeStorageRecommendations(CacheStats stats, CacheRecommendations recommendations)
    {
        if (stats.QuotaUsagePercent > CriticalStorageUtilizationThreshold)
        {
            recommendations.SizeRecommendations.Add("Consider increasing cache quota limit");
            recommendations.SizeRecommendations.Add("Implement more aggressive LRU eviction");
        }
        else if (stats.QuotaUsagePercent > HighStorageUtilizationThreshold)
        {
            recommendations.SizeRecommendations.Add("Monitor storage usage closely");
            recommendations.SizeRecommendations.Add("Consider periodic cleanup scheduling");
        }
    }

    private static void AnalyzeExpirationRecommendations(CacheStats stats, CacheRecommendations recommendations)
    {
        if (stats.TotalExpiredItemsCount > stats.TotalItems * 0.1)
        {
            recommendations.ExpirationRecommendations.Add("Schedule more frequent expired item cleanup");
            recommendations.ExpirationRecommendations.Add("Consider shorter expiration times for volatile data");
        }
    }

    public static void AnalyzePerformanceRecommendations(CacheHealthMetrics healthMetrics, CacheRecommendations recommendations)
    {
        if (healthMetrics.IsMemoryPressureHigh)
        {
            recommendations.PerformanceRecommendations.Add("Reduce cache size to relieve memory pressure");
            recommendations.PerformanceRecommendations.Add("Implement batch operations for better efficiency");
        }

        if (healthMetrics.FragmentationPercent > HighFragmentationThreshold)
        {
            recommendations.PerformanceRecommendations.Add("Perform cache defragmentation");
            recommendations.PerformanceRecommendations.Add("Consider cache reorganization");
        }
    }

    private static void AnalyzeMaintenanceRecommendations(CacheStats stats, CacheHealthMetrics healthMetrics, CacheRecommendations recommendations)
    {
        if (stats.TotalExpiredItemsCount > 0) recommendations.MaintenanceRecommendations.Add("Run cache cleanup immediately");

        if (healthMetrics.StorageUtilizationPercent > HighStorageUtilizationThreshold)
            recommendations.MaintenanceRecommendations.Add("Schedule regular cache optimization");

        if (healthMetrics.PerformanceIssues.Any()) recommendations.MaintenanceRecommendations.Add("Address performance issues identified");
    }

    private static int CalculateHealthScore(CacheHealthMetrics healthMetrics, CacheStats stats)
    {
        var score = 100;

        // Deduct points for storage utilization
        if (healthMetrics.StorageUtilizationPercent > CriticalStorageUtilizationThreshold)
            score -= 40;
        else if (healthMetrics.StorageUtilizationPercent > HighStorageUtilizationThreshold)
            score -= 20;

        // Deduct points for expired items
        if (stats.TotalExpiredItemsCount > 0)
            score -= Math.Min(20, stats.TotalExpiredItemsCount);

        // Deduct points for fragmentation
        if (healthMetrics.FragmentationPercent > HighFragmentationThreshold)
            score -= 15;

        // Deduct points for performance issues
        score -= Math.Min(25, healthMetrics.PerformanceIssues.Count * 5);

        return Math.Max(0, score);
    }

    public async Task<CacheMonitoringStats> GetComprehensiveCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogCollectingComprehensiveStats(_logger, null);

            // Get browser storage statistics
            var storageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);

            // Create basic cache stats from storage stats
            var overallStats = new CacheStats
            {
                TotalItems = storageStats.TotalItems,
                TotalSizeBytes = storageStats.TotalSizeBytes,
                QuotaUsagePercent = storageStats.QuotaUsagePercent,
                QuotaLimitBytes = storageStats.QuotaLimitBytes,
                TotalExpiredItemsCount = 0, // Browser storage doesn't track expired items
                NamespaceStats = new Dictionary<string, CacheNamespaceStats>(StringComparer.Ordinal)
            };

            var result = new CacheMonitoringStats
            {
                Overall = overallStats,
                CacheHitRate = 0.0, // Not available without cache service
                CacheMissRate = 0.0, // Not available without cache service
                AverageAccessTimeMs = 1.0, // Default estimate
                TotalCacheOperations = 0, // Not available without cache service
                CollectedAt = DateTime.UtcNow
            };

            LogComprehensiveStatsCollected(_logger, null);
            return result;
        }
        catch (Exception ex)
        {
            LogFailedToCollectStats(_logger, ex);
            return new CacheMonitoringStats(); // Return empty stats on error
        }
    }

    public async Task<CacheHealthMetrics> GetCacheHealthMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogCollectingHealthMetrics(_logger, null);

            var storageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);

            // Create basic cache stats from storage stats
            var stats = new CacheStats
            {
                TotalItems = storageStats.TotalItems,
                TotalSizeBytes = storageStats.TotalSizeBytes,
                QuotaUsagePercent = storageStats.QuotaUsagePercent,
                QuotaLimitBytes = storageStats.QuotaLimitBytes,
                TotalExpiredItemsCount = 0, // Browser storage doesn't track expired items
                NamespaceStats = new Dictionary<string, CacheNamespaceStats>(StringComparer.Ordinal)
            };

            var storageUtilization = stats.QuotaUsagePercent;
            var healthStatus = DetermineHealthStatus(storageUtilization, stats);
            var performanceIssues = AnalyzePerformanceIssues(stats, storageUtilization);

            var result = new CacheHealthMetrics
            {
                HealthStatus = healthStatus,
                StorageUtilizationPercent = storageUtilization,
                ExpiredItemsCount = stats.TotalExpiredItemsCount,
                IsMemoryPressureHigh = storageUtilization > HighStorageUtilizationThreshold,
                FragmentationPercent = CalculateFragmentationPercent(stats),
                PerformanceIssues = performanceIssues,
                CheckedAt = DateTime.UtcNow
            };

            LogCacheHealthMetricsCollected(_logger, result.HealthStatus, result.StorageUtilizationPercent, null);

            return result;
        }
        catch (Exception ex)
        {
            LogFailedToCollectHealthMetrics(_logger, ex);
            return new CacheHealthMetrics
            {
                HealthStatus = CacheHealthStatus.Error,
                PerformanceIssues = new List<string> { $"Failed to collect metrics: {ex.Message}" }
            };
        }
    }

    public async Task<CachePerformanceStats> GetCachePerformanceStatsAsync(int timeRangeHours = 24, CancellationToken cancellationToken = default)
    {
        try
        {
            LogCollectingPerformanceStats(_logger, timeRangeHours, null);

            var storageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);
            var timeRange = TimeSpan.FromHours(timeRangeHours);

            // Create basic cache stats from storage stats
            var stats = new CacheStats
            {
                TotalItems = storageStats.TotalItems,
                TotalSizeBytes = storageStats.TotalSizeBytes,
                QuotaUsagePercent = storageStats.QuotaUsagePercent,
                QuotaLimitBytes = storageStats.QuotaLimitBytes,
                TotalExpiredItemsCount = 0,
                NamespaceStats = new Dictionary<string, CacheNamespaceStats>(StringComparer.Ordinal)
            };

            // Note: In a real implementation, you would track these metrics over time
            // For now, we'll provide estimates based on current state
            var result = new CachePerformanceStats
            {
                TimeRange = timeRange,
                AverageHitRate = EstimateAverageHitRate(stats),
                PeakStorageUsage = stats.TotalSizeBytes,
                AverageResponseTimeMs = EstimateAverageResponseTime(stats),
                TotalOperations = EstimateTotalOperations(stats, timeRangeHours),
                EvictionsCount = EstimateEvictionsCount(stats),
                CleanupCount = EstimateCleanupCount(stats),
                CollectedAt = DateTime.UtcNow
            };

            LogPerformanceStatsCollected(_logger, timeRangeHours, null);
            return result;
        }
        catch (Exception ex)
        {
            LogFailedToCollectPerformanceStats(_logger, ex);
            return new CachePerformanceStats
            {
                TimeRange = TimeSpan.FromHours(timeRangeHours),
                CollectedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<CacheOptimizationResult> OptimizeCacheAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CacheOptimizationResult();

        try
        {
            LogStartingCacheOptimization(_logger, null);

            // Get initial statistics
            var initialStorageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);
            result.StorageUtilizationBefore = initialStorageStats.QuotaUsagePercent;

            // Phase 1: Clean up expired items (browser storage doesn't track expired items)
            result.ExpiredItemsRemoved = 0;
            result.ActionsPerformed.Add("Browser storage cleanup not applicable");

            // Phase 2: Perform LRU eviction if storage is still high
            if (initialStorageStats.QuotaUsagePercent > HighStorageUtilizationThreshold)
            {
                var targetSize = (long)(initialStorageStats.QuotaLimitBytes * 0.7); // Target 70% usage
                var evictedCount = await _browserStorageService.EvictLeastRecentlyUsedAsync(targetSize, cancellationToken).ConfigureAwait(false);
                result.ItemsEvicted = evictedCount;
                result.ActionsPerformed.Add($"Evicted {evictedCount.ToString(CultureInfo.InvariantCulture)} LRU items");
            }

            // Get final statistics
            var finalStorageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);
            result.StorageUtilizationAfter = finalStorageStats.QuotaUsagePercent;
            result.StorageFreedBytes = initialStorageStats.TotalSizeBytes - finalStorageStats.TotalSizeBytes;
            result.IsSuccessful = true;

            stopwatch.Stop();
            result.OptimizationTimeMs = stopwatch.ElapsedMilliseconds;

            LogCacheOptimizationCompleted(_logger, result.OptimizationTimeMs, result.StorageUtilizationBefore, result.StorageUtilizationAfter, null);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.OptimizationTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsSuccessful = false;
            result.ActionsPerformed.Add($"Optimization failed: {ex.Message}");

            LogCacheOptimizationFailedWithTime(_logger, result.OptimizationTimeMs, ex);
            return result;
        }
    }

    public async Task<CacheRecommendations> GetCacheRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogGeneratingCacheRecommendations(_logger, null);

            var storageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);

            // Create basic cache stats from storage stats
            var stats = new CacheStats
            {
                TotalItems = storageStats.TotalItems,
                TotalSizeBytes = storageStats.TotalSizeBytes,
                QuotaUsagePercent = storageStats.QuotaUsagePercent,
                QuotaLimitBytes = storageStats.QuotaLimitBytes,
                TotalExpiredItemsCount = 0,
                NamespaceStats = new Dictionary<string, CacheNamespaceStats>(StringComparer.Ordinal)
            };

            var healthMetrics = await GetCacheHealthMetricsAsync(cancellationToken).ConfigureAwait(false);

            var recommendations = new CacheRecommendations();

            // Storage recommendations
            AnalyzeStorageRecommendations(stats, recommendations);

            // Expiration recommendations
            AnalyzeExpirationRecommendations(stats, recommendations);

            // Performance recommendations
            AnalyzePerformanceRecommendations(healthMetrics, recommendations);

            // Maintenance recommendations
            AnalyzeMaintenanceRecommendations(stats, healthMetrics, recommendations);

            // Calculate overall health score
            recommendations.HealthScore = CalculateHealthScore(healthMetrics, stats);

            LogCacheRecommendationsGenerated(_logger, recommendations.HealthScore, null);
            return recommendations;
        }
        catch (Exception ex)
        {
            LogFailedToGenerateRecommendations(_logger, ex);
            return new CacheRecommendations
            {
                MaintenanceRecommendations = new List<string> { $"Error generating recommendations: {ex.Message}" },
                HealthScore = 0
            };
        }
    }
}