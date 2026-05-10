using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     TUnit tests for CacheMonitoringService.
/// </summary>
[Category("Feature:Services")]
[Category("Unit")]
public sealed partial class CacheMonitoringServiceTests
{
    [Test]
    public async Task GetComprehensiveCacheStatsAsync_Should_Map_Storage_Stats()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.BrowserStorageService.QueueStorageStats(
            new StorageStats
            {
                TotalItems = 12,
                TotalSizeBytes = 2400,
                QuotaLimitBytes = 10000,
                QuotaUsagePercent = 24.0
            });

        // Act
        var result = await scope.Service.GetComprehensiveCacheStatsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result.Overall.TotalItems).IsEqualTo(12);
        await Assert.That(result.Overall.TotalSizeBytes).IsEqualTo(2400);
        await Assert.That(result.Overall.QuotaLimitBytes).IsEqualTo(10000);
        await Assert.That(result.Overall.QuotaUsagePercent).IsEqualTo(24.0);
        await Assert.That(result.CacheHitRate).IsEqualTo(0.0);
        await Assert.That(result.CacheMissRate).IsEqualTo(0.0);
        await Assert.That(result.AverageAccessTimeMs).IsEqualTo(1.0);
        await Assert.That(result.TotalCacheOperations).IsEqualTo(0);
        await Assert.That(result.CollectedAt).IsNotEqualTo(default(DateTime));
    }

    [Test]
    public async Task GetCacheHealthMetricsAsync_Should_Return_Critical_When_Utilization_Is_High()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.BrowserStorageService.QueueStorageStats(
            new StorageStats
            {
                TotalItems = 12,
                TotalSizeBytes = 9500,
                QuotaLimitBytes = 10000,
                QuotaUsagePercent = 95.0
            });

        // Act
        var result = await scope.Service.GetCacheHealthMetricsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result.HealthStatus).IsEqualTo(CacheHealthStatus.Critical);
        await Assert.That(result.StorageUtilizationPercent).IsEqualTo(95.0);
        await Assert.That(result.IsMemoryPressureHigh).IsTrue();
        await Assert.That(result.FragmentationPercent).IsEqualTo(0.0);
        await Assert.That(result.PerformanceIssues).Contains("Critical storage utilization: 95.00%");
    }

    [Test]
    public async Task OptimizeCacheAsync_Should_Evict_When_Storage_Usage_Is_High()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.BrowserStorageService.QueueStorageStats(
            new StorageStats
            {
                TotalItems = 100,
                TotalSizeBytes = 9500,
                QuotaLimitBytes = 10000,
                QuotaUsagePercent = 95.0
            },
            new StorageStats
            {
                TotalItems = 70,
                TotalSizeBytes = 7000,
                QuotaLimitBytes = 10000,
                QuotaUsagePercent = 70.0
            });
        scope.BrowserStorageService.EvictionResult = 3;

        // Act
        var result = await scope.Service.OptimizeCacheAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result.IsSuccessful).IsTrue();
        await Assert.That(result.StorageUtilizationBefore).IsEqualTo(95.0);
        await Assert.That(result.StorageUtilizationAfter).IsEqualTo(70.0);
        await Assert.That(result.ItemsEvicted).IsEqualTo(3);
        await Assert.That(result.StorageFreedBytes).IsEqualTo(2500);
        await Assert.That(result.ActionsPerformed).Contains("Browser storage cleanup not applicable");
        await Assert.That(result.ActionsPerformed).Contains("Evicted 3 LRU items");
        await Assert.That(scope.BrowserStorageService.LastEvictionTargetSize).IsEqualTo(7000);
    }

    [Test]
    public async Task GetCacheHealthMetricsAsync_Should_Return_Error_When_Storage_Service_Fails()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.BrowserStorageService.GetStorageStatsException = new InvalidOperationException("boom");

        // Act
        var result = await scope.Service.GetCacheHealthMetricsAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result.HealthStatus).IsEqualTo(CacheHealthStatus.Error);
        await Assert.That(result.PerformanceIssues).Contains("Failed to collect metrics: boom");
    }

    // ────────────────────────────────────────
    // DetermineHealthStatus (pure function, CRAP 9.3 → tested directly)
    // ────────────────────────────────────────

    [Test]
    public async Task DetermineHealthStatus_ReturnsCritical_WhenStorageUtilizationAbove90()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var result = CacheMonitoringService.DetermineHealthStatus(91, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Critical);
    }

    [Test]
    public async Task DetermineHealthStatus_ReturnsWarning_WhenStorageUtilizationAbove80()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var result = CacheMonitoringService.DetermineHealthStatus(81, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Warning);
    }

    [Test]
    public async Task DetermineHealthStatus_ReturnsWarning_WhenExpiredItemsExceed10Percent()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 11 };
        var result = CacheMonitoringService.DetermineHealthStatus(75, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Warning);
    }

    [Test]
    public async Task DetermineHealthStatus_ReturnsHealthy_WhenStorageAndExpiryAreNormal()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 10 };
        var result = CacheMonitoringService.DetermineHealthStatus(75, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Healthy);
    }

    [Test]
    public async Task DetermineHealthStatus_ExactThreshold_90_IsNotCritical()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var result = CacheMonitoringService.DetermineHealthStatus(90, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Warning);
    }

    [Test]
    public async Task DetermineHealthStatus_ExactThreshold_80_FallsThroughToExpiredCheck()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var result = CacheMonitoringService.DetermineHealthStatus(80, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Healthy);
    }

    [Test]
    public async Task DetermineHealthStatus_ZeroItems_WithHighStorage_ReturnsCritical()
    {
        var stats = new CacheStats { TotalItems = 0, TotalExpiredItemsCount = 0 };
        var result = CacheMonitoringService.DetermineHealthStatus(95, stats);
        await Assert.That(result).IsEqualTo(CacheHealthStatus.Critical);
    }

    // ────────────────────────────────────────
    // AnalyzePerformanceIssues (pure function, CRAP 8.1 → tested directly)
    // ────────────────────────────────────────

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsCritical_WhenStorageUtilizationAbove90()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 95);
        await Assert.That(issues).Contains("Critical storage utilization: 95.00%");
    }

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsHigh_WhenStorageUtilizationAbove80()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 85);
        await Assert.That(issues).Contains("High storage utilization: 85.00%");
    }

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsExpiredItemsWarning_WhenExpiredExist()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 5 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 75);
        await Assert.That(issues).Contains("5 expired items need cleanup");
    }

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsFragmentationWarning_WhenFragmentationAbove30()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 31 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 75);
        await Assert.That(issues).Contains("High cache fragmentation: 31.00%");
    }

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsEmpty_WhenNoIssues()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 75);
        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task AnalyzePerformanceIssues_ReturnsAllIssues_WhenEverythingIsWrong()
    {
        var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 31 };
        var issues = CacheMonitoringService.AnalyzePerformanceIssues(stats, 95);
        await Assert.That(issues).Contains("Critical storage utilization: 95.00%");
        await Assert.That(issues).Contains("31 expired items need cleanup");
        await Assert.That(issues).Contains("High cache fragmentation: 31.00%");
    }

    // ────────────────────────────────────────
    // AnalyzeStorageRecommendations (CC=3, 0% → tested)
    // ────────────────────────────────────────

    [Test]
    public async Task AnalyzeStorageRecommendations_AddsCriticalRecommendations_WhenAboveCriticalThreshold()
    {
        var stats = new CacheStats { QuotaUsagePercent = 95 };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzeStorageRecommendations(stats, recommendations);
        await Assert.That(recommendations.SizeRecommendations).Contains("Consider increasing cache quota limit");
        await Assert.That(recommendations.SizeRecommendations).Contains("Implement more aggressive LRU eviction");
    }

    [Test]
    public async Task AnalyzeStorageRecommendations_AddsHighRecommendations_WhenAboveHighThreshold()
    {
        var stats = new CacheStats { QuotaUsagePercent = 85 };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzeStorageRecommendations(stats, recommendations);
        await Assert.That(recommendations.SizeRecommendations).Contains("Monitor storage usage closely");
        await Assert.That(recommendations.SizeRecommendations).Contains("Consider periodic cleanup scheduling");
    }

    [Test]
    public async Task AnalyzeStorageRecommendations_AddsNothing_WhenBelowThresholds()
    {
        var stats = new CacheStats { QuotaUsagePercent = 75 };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzeStorageRecommendations(stats, recommendations);
        await Assert.That(recommendations.SizeRecommendations).IsEmpty();
    }

    // ────────────────────────────────────────
    // AnalyzePerformanceRecommendations (CC=3, 0% → tested)
    // ────────────────────────────────────────

    [Test]
    public async Task AnalyzePerformanceRecommendations_AddsMemoryPressureRecommendations_WhenMemoryIsHigh()
    {
        var healthMetrics = new CacheHealthMetrics { IsMemoryPressureHigh = true };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzePerformanceRecommendations(healthMetrics, recommendations);
        await Assert.That(recommendations.PerformanceRecommendations).Contains("Reduce cache size to relieve memory pressure");
        await Assert.That(recommendations.PerformanceRecommendations).Contains("Implement batch operations for better efficiency");
    }

    [Test]
    public async Task AnalyzePerformanceRecommendations_AddsFragmentationRecommendations_WhenFragmentationIsHigh()
    {
        var healthMetrics = new CacheHealthMetrics { FragmentationPercent = 35 };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzePerformanceRecommendations(healthMetrics, recommendations);
        await Assert.That(recommendations.PerformanceRecommendations).Contains("Perform cache defragmentation");
        await Assert.That(recommendations.PerformanceRecommendations).Contains("Consider cache reorganization");
    }

    [Test]
    public async Task AnalyzePerformanceRecommendations_AddsAllRecommendations_WhenBothIssuesPresent()
    {
        var healthMetrics = new CacheHealthMetrics { IsMemoryPressureHigh = true, FragmentationPercent = 35 };
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzePerformanceRecommendations(healthMetrics, recommendations);
        await Assert.That(recommendations.PerformanceRecommendations.Count).IsEqualTo(4);
    }

    [Test]
    public async Task AnalyzePerformanceRecommendations_AddsNothing_WhenNoIssues()
    {
        var healthMetrics = new CacheHealthMetrics();
        var recommendations = new CacheRecommendations();
        CacheMonitoringService.AnalyzePerformanceRecommendations(healthMetrics, recommendations);
        await Assert.That(recommendations.PerformanceRecommendations).IsEmpty();
    }
}
