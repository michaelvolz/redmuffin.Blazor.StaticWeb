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
    private const double LowCacheHitRateThreshold = 60.0;
    private const double CriticalCacheHitRateThreshold = 40.0;
    private readonly IBrowserStorageService _browserStorageService;
    private readonly ICacheService _cacheService;
    private readonly IImageValidationService _imageValidationService;
    private readonly ILogger<CacheMonitoringService> _logger;
    private readonly IOpenGraphImagesService _openGraphImagesService;

    public CacheMonitoringService(
        ICacheService cacheService,
        IOpenGraphImagesService openGraphImagesService,
        IImageValidationService imageValidationService,
        IBrowserStorageService browserStorageService,
        ILogger<CacheMonitoringService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _openGraphImagesService = openGraphImagesService ?? throw new ArgumentNullException(nameof(openGraphImagesService));
        _imageValidationService = imageValidationService ?? throw new ArgumentNullException(nameof(imageValidationService));
        _browserStorageService = browserStorageService ?? throw new ArgumentNullException(nameof(browserStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CacheMonitoringStats> GetComprehensiveCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Collecting comprehensive cache statistics");

            // Get overall cache statistics
            var overallStats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);

            // Get OpenGraph cache statistics
            var openGraphStats = await _openGraphImagesService.GetCacheStatsAsync().ConfigureAwait(false);

            // Get image validation cache statistics
            var imageValidationStats = await _imageValidationService.GetValidationCacheStatsAsync(cancellationToken).ConfigureAwait(false);

            // Calculate cache hit/miss rates
            var totalAccesses = CalculateTotalAccesses(overallStats);
            var (hitRate, missRate) = CalculateHitMissRates(overallStats, totalAccesses);

            var result = new CacheMonitoringStats
            {
                Overall = overallStats,
                OpenGraphStats = openGraphStats,
                ImageValidationStats = imageValidationStats,
                CacheHitRate = hitRate,
                CacheMissRate = missRate,
                AverageAccessTimeMs = CalculateAverageAccessTime(overallStats),
                TotalCacheOperations = totalAccesses,
                CollectedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Comprehensive cache statistics collected successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect comprehensive cache statistics");
            return new CacheMonitoringStats(); // Return empty stats on error
        }
    }

    public async Task<CacheHealthMetrics> GetCacheHealthMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Collecting cache health metrics");

            var stats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
            var storageStats = await _browserStorageService.GetStorageStatsAsync(cancellationToken).ConfigureAwait(false);

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

            _logger.LogInformation("Cache health metrics collected: Status={HealthStatus}, StorageUtilization={StorageUtilization:F2}%",
                result.HealthStatus, result.StorageUtilizationPercent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect cache health metrics");
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
            _logger.LogDebug("Collecting cache performance statistics for {TimeRange} hours", timeRangeHours);

            var stats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
            var timeRange = TimeSpan.FromHours(timeRangeHours);

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

            _logger.LogInformation("Cache performance statistics collected for {TimeRange} hours", timeRangeHours);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect cache performance statistics");
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
            _logger.LogInformation("Starting cache optimization");

            // Get initial statistics
            var initialStats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
            result.StorageUtilizationBefore = initialStats.QuotaUsagePercent;

            // Phase 1: Clean up expired items
            var expiredCleaned = await _cacheService.CleanupExpiredItemsAsync(cancellationToken).ConfigureAwait(false);
            result.ExpiredItemsRemoved = expiredCleaned;
            result.ActionsPerformed.Add($"Cleaned up {expiredCleaned.ToString(CultureInfo.InvariantCulture)} expired items");

            // Phase 2: Perform LRU eviction if storage is still high
            var finalStats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
            if (finalStats.QuotaUsagePercent > HighStorageUtilizationThreshold)
            {
                var targetSize = (long)(finalStats.QuotaLimitBytes * 0.7); // Target 70% usage
                var evictedCount = await _browserStorageService.EvictLeastRecentlyUsedAsync(targetSize, cancellationToken).ConfigureAwait(false);
                result.ItemsEvicted = evictedCount;
                result.ActionsPerformed.Add($"Evicted {evictedCount.ToString(CultureInfo.InvariantCulture)} LRU items");
            }

            // Get final statistics
            var optimizedStats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
            result.StorageUtilizationAfter = optimizedStats.QuotaUsagePercent;
            result.StorageFreedBytes = initialStats.TotalSizeBytes - optimizedStats.TotalSizeBytes;
            result.IsSuccessful = true;

            stopwatch.Stop();
            result.OptimizationTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Cache optimization completed successfully in {ElapsedMs}ms. " +
                                   "Storage utilization: {Before:F2}% → {After:F2}%",
                result.OptimizationTimeMs, result.StorageUtilizationBefore, result.StorageUtilizationAfter);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.OptimizationTimeMs = stopwatch.ElapsedMilliseconds;
            result.IsSuccessful = false;
            result.ActionsPerformed.Add($"Optimization failed: {ex.Message}");

            _logger.LogError(ex, "Cache optimization failed after {ElapsedMs}ms", result.OptimizationTimeMs);
            return result;
        }
    }

    public async Task<CacheRecommendations> GetCacheRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating cache recommendations");

            var stats = await _cacheService.GetCacheStatsAsync(cancellationToken).ConfigureAwait(false);
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

            _logger.LogInformation("Cache recommendations generated with health score: {HealthScore}", recommendations.HealthScore);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate cache recommendations");
            return new CacheRecommendations
            {
                MaintenanceRecommendations = new List<string> { $"Error generating recommendations: {ex.Message}" },
                HealthScore = 0
            };
        }
    }

    private long CalculateTotalAccesses(CacheStats stats)
    {
        // Estimate total accesses based on namespace statistics
        return stats.NamespaceStats.Values.Sum(ns => (long)(ns.TotalItems * ns.AverageAccessCount));
    }

    private (double HitRate, double MissRate) CalculateHitMissRates(CacheStats stats, long totalAccesses)
    {
        if (totalAccesses == 0) return (0.0, 0.0);

        // Estimate hit rate based on cache efficiency
        var hitRate = Math.Min(95.0, 60.0 + stats.TotalItems * 0.01);
        var missRate = 100.0 - hitRate;

        return (hitRate, missRate);
    }

    private double CalculateAverageAccessTime(CacheStats stats)
    {
        // Estimate average access time based on storage utilization
        var baseTime = 1.0; // Base access time in milliseconds
        var utilizationFactor = stats.QuotaUsagePercent / 100.0;
        return baseTime * (1.0 + utilizationFactor);
    }

    private CacheHealthStatus DetermineHealthStatus(double storageUtilization, CacheStats stats)
    {
        if (storageUtilization > CriticalStorageUtilizationThreshold)
            return CacheHealthStatus.Critical;

        if (storageUtilization > HighStorageUtilizationThreshold)
            return CacheHealthStatus.Warning;

        if (stats.TotalExpiredItemsCount > stats.TotalItems * 0.1)
            return CacheHealthStatus.Warning;

        return CacheHealthStatus.Healthy;
    }

    private List<string> AnalyzePerformanceIssues(CacheStats stats, double storageUtilization)
    {
        var issues = new List<string>();

        if (storageUtilization > CriticalStorageUtilizationThreshold)
            issues.Add($"Critical storage utilization: {storageUtilization:F2}%");
        else if (storageUtilization > HighStorageUtilizationThreshold)
            issues.Add($"High storage utilization: {storageUtilization:F2}%");

        if (stats.TotalExpiredItemsCount > 0)
            issues.Add($"{stats.TotalExpiredItemsCount} expired items need cleanup");

        var fragmentationPercent = CalculateFragmentationPercent(stats);
        if (fragmentationPercent > HighFragmentationThreshold)
            issues.Add($"High cache fragmentation: {fragmentationPercent:F2}%");

        return issues;
    }

    private double CalculateFragmentationPercent(CacheStats stats)
    {
        if (stats.TotalItems == 0) return 0.0;

        // Estimate fragmentation based on expired items ratio
        var expiredRatio = (double)stats.TotalExpiredItemsCount / stats.TotalItems;
        return Math.Min(100.0, expiredRatio * 100.0);
    }

    private double EstimateAverageHitRate(CacheStats stats)
    {
        // Estimate based on cache efficiency
        return Math.Min(95.0, 60.0 + stats.TotalItems * 0.01);
    }

    private double EstimateAverageResponseTime(CacheStats stats)
    {
        return CalculateAverageAccessTime(stats);
    }

    private long EstimateTotalOperations(CacheStats stats, int timeRangeHours)
    {
        // Estimate based on current activity
        var operationsPerHour = stats.TotalItems * 10; // Rough estimate
        return operationsPerHour * timeRangeHours;
    }

    private int EstimateEvictionsCount(CacheStats stats)
    {
        // Estimate based on storage pressure
        return stats.QuotaUsagePercent > HighStorageUtilizationThreshold ? (int)(stats.TotalItems * 0.1) : 0;
    }

    private int EstimateCleanupCount(CacheStats stats)
    {
        // Estimate based on expired items
        return stats.TotalExpiredItemsCount > 0 ? 1 : 0;
    }

    private void AnalyzeStorageRecommendations(CacheStats stats, CacheRecommendations recommendations)
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

    private void AnalyzeExpirationRecommendations(CacheStats stats, CacheRecommendations recommendations)
    {
        if (stats.TotalExpiredItemsCount > stats.TotalItems * 0.1)
        {
            recommendations.ExpirationRecommendations.Add("Schedule more frequent expired item cleanup");
            recommendations.ExpirationRecommendations.Add("Consider shorter expiration times for volatile data");
        }
    }

    private void AnalyzePerformanceRecommendations(CacheHealthMetrics healthMetrics, CacheRecommendations recommendations)
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

    private void AnalyzeMaintenanceRecommendations(CacheStats stats, CacheHealthMetrics healthMetrics, CacheRecommendations recommendations)
    {
        if (stats.TotalExpiredItemsCount > 0) recommendations.MaintenanceRecommendations.Add("Run cache cleanup immediately");

        if (healthMetrics.StorageUtilizationPercent > HighStorageUtilizationThreshold)
            recommendations.MaintenanceRecommendations.Add("Schedule regular cache optimization");

        if (healthMetrics.PerformanceIssues.Any()) recommendations.MaintenanceRecommendations.Add("Address performance issues identified");
    }

    private int CalculateHealthScore(CacheHealthMetrics healthMetrics, CacheStats stats)
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
}