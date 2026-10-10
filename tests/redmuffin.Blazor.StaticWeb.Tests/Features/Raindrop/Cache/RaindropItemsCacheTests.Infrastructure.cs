using System.Text.Json;
using LZStringCSharp;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Enums;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Cache;

[Category("Feature:Cache")]
public sealed partial class RaindropItemsCacheTests
{
    [Test]
    public async Task CacheOperations_ConcurrentAccess_MaintainsDataIntegrity()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testData1 = CreateTestRaindropItems();
        var testData2 = CreateTestRaindropItems();

        // Act - Perform concurrent operations
        var tasks = new List<Task>
        {
            scope.Cache.SetAsync("videos", testData1, CancellationToken.None),
            scope.Cache.SetAsync("articles", testData2, CancellationToken.None),
            scope.Cache.GetAsync("videos", CancellationToken.None),
            scope.Cache.IsExpiredAsync("videos", CancellationToken.None),
        };

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Assert - Verify data integrity
        var result1 = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);
        var result2 = await scope
            .Cache.GetAsync("articles", CancellationToken.None)
            .ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result1.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result2.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result1.Data).Count().IsEqualTo(2);
            await Assert.That(result2.Data).Count().IsEqualTo(2);
        }
    }

    [Test]
    public async Task CompressionDecompression_SpecialCharacters_PreservesEncoding()
    {
        // Arrange
        using var scope = CreateTestScope();
        var specialCharItems = CreateTestItemsWithSpecialCharacters();
        var jsonData = JsonSerializer.Serialize(specialCharItems, scope.JsonOptions);

        // Act - Compress and decompress data with special characters
        var compressedData = LZString.CompressToUTF16(jsonData);
        var decompressedData = LZString.DecompressFromUTF16(compressedData);
        var deserializedItems = JsonSerializer.Deserialize<List<RaindropItem>>(
            decompressedData!,
            scope.JsonOptions
        );

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(compressedData).IsNotNull();
            await Assert.That(decompressedData).IsEqualTo(jsonData);
            await Assert
                .That(deserializedItems![0].Title)
                .IsEqualTo("Test with émojis 🚀 and spëcial chars");
            await Assert
                .That(deserializedItems[0].Excerpt)
                .IsEqualTo("Content with 中文 and العربية text");
        }
    }

    [Test]
    public async Task GetAsync_CacheExpirationBoundary_ReturnsExpiredWhenExactlyExpired()
    {
        // Arrange
        using var scope = CreateTestScope();
        var exactlyExpiredMetadata = new RaindropCacheMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-28),
            LastAccessedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAt = DateTimeOffset.UtcNow, // Exactly now
            Version = "1.0",
            ItemCount = 2,
            CompressedSize = 1024,
            OriginalSize = 2048,
        };

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            exactlyExpiredMetadata
        );

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Expired);
    }

    [Test]
    public async Task GetAsync_CacheExpirationBoundary_ReturnsHitWhenNotYetExpired()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();
        var jsonData = JsonSerializer.Serialize(testItems, scope.JsonOptions);
        var compressedData = LZString.CompressToUTF16(jsonData);
        var notYetExpiredMetadata = new RaindropCacheMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-27),
            LastAccessedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1), // Expires in 1 minute
            Version = "1.0",
            ItemCount = 2,
            CompressedSize = 1024,
            OriginalSize = 2048,
        };

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            notYetExpiredMetadata
        );
        scope.LocalStorageService_Mock.SetupGetItemAsync("raindrop_cache_videos", compressedData);

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
    }

    [Test]
    public async Task GetAsync_ExpiredCache_ReturnsExpired()
    {
        // Arrange
        using var scope = CreateTestScope();
        var expiredMetadata = CreateExpiredTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            expiredMetadata
        );

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Expired);
    }

    [Test]
    public async Task GetAsync_MetadataExistsButDataMissing_ReturnsMiss()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            metadata
        );
        scope.LocalStorageService_Mock.SetupGetItemAsync<string>("raindrop_cache_videos", null); // Data missing

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Miss);
    }

    [Test]
    public async Task GetAsync_ValidCachedData_ReturnsSuccess()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();

        // Create valid compressed data by actually compressing the test items
        var jsonData = JsonSerializer.Serialize(testItems, scope.JsonOptions);
        var compressedData = LZString.CompressToUTF16(jsonData);
        var metadata = CreateTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            metadata
        );
        scope.LocalStorageService_Mock.SetupGetItemAsync("raindrop_cache_videos", compressedData);

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).IsNotNull();
            await Assert.That(result.Metadata).IsNotNull();
        }
    }

    [Test]
    public async Task IsExpiredAsync_ExpiredCache_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        var expiredMetadata = CreateExpiredTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            expiredMetadata
        );

        // Act
        var result = await scope
            .Cache.IsExpiredAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_NoMetadata_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();

        scope.LocalStorageService_Mock.SetupContainKeyAsync(
            "raindrop_cache_videos_metadata",
            false
        );

        // Act
        var result = await scope
            .Cache.IsExpiredAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_ValidCache_ReturnsFalse()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync(
            "raindrop_cache_videos_metadata",
            metadata
        );

        // Act
        var result = await scope
            .Cache.IsExpiredAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task SetAsync_LargeDataSet_RoundTrips_To_Hit()
    {
        // Arrange
        using var scope = CreateTestScope();
        var largeDataSet = CreatePerformanceTestDataSet();

        // Act
        await scope
            .Cache.SetAsync("videos", largeDataSet, CancellationToken.None)
            .ConfigureAwait(false);
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).Count().IsEqualTo(largeDataSet.Count);
            await Assert.That(result.Data![0].Title).IsEqualTo(largeDataSet[0].Title);
        }
    }
}
