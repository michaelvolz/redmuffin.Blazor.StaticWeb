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
}
