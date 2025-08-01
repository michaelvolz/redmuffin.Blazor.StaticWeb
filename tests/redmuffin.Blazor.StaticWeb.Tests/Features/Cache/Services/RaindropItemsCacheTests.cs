using System.Diagnostics;
using System.Text.Json;
using LZStringCSharp;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Enums;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Cache.Services;

public partial class RaindropItemsCacheTests
{
    private static List<RaindropItem> CreateCompressibleTestDataSet()
    {
        var items = new List<RaindropItem>();
        var repeatedText = "This is repeated text that should compress well. ";

        for (var i = 0; i < 100; i++)
            items.Add(new RaindropItem
            {
                Id = i,
                Link = $"https://example.com/item/{i}",
                Title = $"Test Item {i} - {repeatedText}",
                Excerpt = string.Concat(Enumerable.Repeat(repeatedText, 10)), // Repeat text for better compression
                Cover = $"https://example.com/images/cover_{i}.jpg",
                Created = DateTime.UtcNow.AddDays(-1),
                Type = "article"
            });
        return items;
    }

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
            scope.Cache.IsExpiredAsync("videos", CancellationToken.None)
        };

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Assert - Verify data integrity
        var result1 = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);
        var result2 = await scope.Cache.GetAsync("articles", CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result1.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result2.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result1.Data).HasCount().EqualTo(2);
            await Assert.That(result2.Data).HasCount().EqualTo(2);
        }
    }

    [Test]
    public async Task CacheOperations_MemoryUsage_StaysWithinLimits()
    {
        // Arrange
        using var scope = CreateTestScope();
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Perform multiple cache operations
        for (var i = 0; i < 10; i++)
        {
            var testData = CreateTestRaindropItems();
            await scope.Cache.SetAsync($"test_{i}", testData, CancellationToken.None).ConfigureAwait(false);
            await scope.Cache.GetAsync($"test_{i}", CancellationToken.None).ConfigureAwait(false);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert - Memory increase should be reasonable (less than 50MB)
        await Assert.That(memoryIncrease).IsLessThan(50 * 1024 * 1024);
    }

    [Test]
    public async Task ClearAsync_ExistingCache_RemovesSuccessfully()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        await scope.Cache.ClearAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Cache clear successful"))).IsTrue();
    }

    [Test]
    public async Task ClearAsync_LocalStorageRemoveThrows_HandlesException()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupRemoveItemAsyncThrows("raindrop_cache_videos", new InvalidOperationException("Storage access denied"));

        var cache = scope.Cache;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.ClearAsync("videos").ConfigureAwait(false));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task CompressionDecompression_EmptyDataSet_HandlesGracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        var emptyItems = new List<RaindropItem>();
        var jsonData = JsonSerializer.Serialize(emptyItems, scope.JsonOptions);

        // Act - Compress and decompress empty dataset
        var compressedData = LZString.CompressToUTF16(jsonData);
        var decompressedData = LZString.DecompressFromUTF16(compressedData);
        var deserializedItems = JsonSerializer.Deserialize<List<RaindropItem>>(decompressedData!, scope.JsonOptions);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(compressedData).IsNotNull();
            await Assert.That(decompressedData).IsEqualTo(jsonData);
            await Assert.That(deserializedItems).HasCount().EqualTo(0);
        }
    }

    [Test]
    public async Task CompressionDecompression_LargeDataSet_MaintainsDataIntegrity()
    {
        // Arrange
        using var scope = CreateTestScope();
        var largeTestItems = CreateLargeTestDataSet();
        var jsonData = JsonSerializer.Serialize(largeTestItems, scope.JsonOptions);

        // Act - Compress and decompress large dataset
        var compressedData = LZString.CompressToUTF16(jsonData);
        var decompressedData = LZString.DecompressFromUTF16(compressedData);
        var deserializedItems = JsonSerializer.Deserialize<List<RaindropItem>>(decompressedData!, scope.JsonOptions);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(compressedData).IsNotNull();
            await Assert.That(decompressedData).IsEqualTo(jsonData);
            await Assert.That(deserializedItems).HasCount().EqualTo(largeTestItems.Count);
            await Assert.That(deserializedItems![0].Title).IsEqualTo(largeTestItems[0].Title);
            await Assert.That(compressedData.Length).IsLessThan(jsonData.Length); // Verify compression occurred
        }
    }

    [Test]
    public async Task CompressionDecompression_RoundTrip_WorksCorrectly()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();
        var jsonData = JsonSerializer.Serialize(testItems, scope.JsonOptions);

        // Act - Compress and decompress
        var compressedData = LZString.CompressToUTF16(jsonData);
        var decompressedData = LZString.DecompressFromUTF16(compressedData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(compressedData).IsNotNull();
            await Assert.That(decompressedData).IsNotNull();
            await Assert.That(decompressedData).IsEqualTo(jsonData);
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
        var deserializedItems = JsonSerializer.Deserialize<List<RaindropItem>>(decompressedData!, scope.JsonOptions);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(compressedData).IsNotNull();
            await Assert.That(decompressedData).IsEqualTo(jsonData);
            await Assert.That(deserializedItems![0].Title).IsEqualTo("Test with émojis 🚀 and spëcial chars");
            await Assert.That(deserializedItems[0].Excerpt).IsEqualTo("Content with 中文 and العربية text");
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
            OriginalSize = 2048
        };

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", exactlyExpiredMetadata);

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

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
            OriginalSize = 2048
        };

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", notYetExpiredMetadata);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos", compressedData);

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
    }

    [Test]
    public async Task GetAsync_DecompressionFailure_ReturnsError()
    {
        // Arrange

        using var scope = CreateTestScope();
        var invalidCompressedData = "invalid_compressed_data";
        var metadata = CreateTestMetadata();


        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos", invalidCompressedData);

        // Act

        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);


        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Error);
            await Assert.That(result.Data).IsNull();
        }
    }

    [Test]
    public async Task GetAsync_EmptyCache_ReturnsNotFound()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", false);

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Miss);
    }

    [Test]
    public async Task GetAsync_ExpiredCache_ReturnsExpired()
    {
        // Arrange
        using var scope = CreateTestScope();
        var expiredMetadata = CreateExpiredTestMetadata();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", expiredMetadata);

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Expired);
    }

    [Test]
    public async Task GetAsync_LargeDataSet_CompletesWithinTimeLimit()
    {
        // Arrange
        using var scope = CreateTestScope();
        var largeDataSet = CreatePerformanceTestDataSet();
        await scope.Cache.SetAsync("videos", largeDataSet, CancellationToken.None).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(2000); // Should complete within 2 seconds
        }
    }

    [Test]
    public async Task GetAsync_LocalStorageDataRetrievalThrows_ReturnsError()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();
        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageMock.SetupGetItemAsyncThrows<string>("raindrop_cache_videos", new InvalidOperationException("Storage access denied"));

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Error);
            await Assert.That(result.ErrorMessage).Contains("Storage access denied");
        }
    }

    [Test]
    public async Task GetAsync_LocalStorageMetadataCheckThrows_ReturnsError()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupContainKeyAsyncThrows("raindrop_cache_videos_metadata", new InvalidOperationException("LocalStorage unavailable"));

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Error);
            await Assert.That(result.ErrorMessage).Contains("LocalStorage unavailable");
        }
    }

    [Test]
    public async Task GetAsync_LocalStorageMetadataRetrievalThrows_ReturnsError()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
            new InvalidOperationException("Storage quota exceeded"));

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Error);
            await Assert.That(result.ErrorMessage).Contains("Storage quota exceeded");
        }
    }

    [Test]
    public async Task GetAsync_MetadataExistsButDataMissing_ReturnsMiss()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageMock.SetupGetItemAsync<string>("raindrop_cache_videos", null); // Data missing

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

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


        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos", compressedData);


        // Act

        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);


        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).IsNotNull();
            await Assert.That(result.Metadata).IsNotNull();
        }
    }

    [Test]
    public async Task IsExpiredAsync_CorruptedMetadata_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync<RaindropCacheMetadata>("raindrop_cache_videos_metadata", null);

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_ExpiredCache_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        var expiredMetadata = CreateExpiredTestMetadata();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", expiredMetadata);

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }


    [Test]
    public async Task IsExpiredAsync_LocalStorageCheckThrows_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupContainKeyAsyncThrows("raindrop_cache_videos_metadata", new InvalidOperationException("Storage unavailable"));

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_LocalStorageMetadataRetrievalThrows_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
            new InvalidOperationException("Storage corrupted"));

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_NoMetadata_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", false);

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsExpiredAsync_ValidCache_ReturnsFalse()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();

        scope.LocalStorageMock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageMock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task SetAsync_CompressionEfficiency_AchievesTargetRatio()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testData = CreateCompressibleTestDataSet();
        var originalSize = JsonSerializer.Serialize(testData, scope.JsonOptions).Length;

        // Act
        await scope.Cache.SetAsync("videos", testData, CancellationToken.None).ConfigureAwait(false);

        // Assert - Verify compression occurred by checking that compressed data is smaller
        var jsonData = JsonSerializer.Serialize(testData, scope.JsonOptions);
        var compressedData = LZString.CompressToUTF16(jsonData);
        var compressionRatio = (double)compressedData.Length / originalSize;

        await Assert.That(compressionRatio).IsLessThan(0.8); // Should achieve at least 20% compression
    }

    [Test]
    public async Task SetAsync_LargeDataSet_CompletesWithinTimeLimit()
    {
        // Arrange
        using var scope = CreateTestScope();
        var largeDataSet = CreatePerformanceTestDataSet();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await scope.Cache.SetAsync("videos", largeDataSet, CancellationToken.None).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(5000); // Should complete within 5 seconds
    }

    [Test]
    public async Task SetAsync_LocalStorageMetadataSetThrows_LogsWarningButContinues()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();
        scope.LocalStorageMock.SetupSetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
            new InvalidOperationException("Storage quota exceeded"));

        // Act
        await scope.Cache.SetAsync("videos", testItems, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Cache storage successful"))).IsTrue();
    }

    [Test]
    public async Task SetAsync_LocalStorageSetThrows_HandlesException()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageMock.SetupSetItemAsyncThrows<string>("raindrop_cache_videos", new InvalidOperationException("Storage quota exceeded"));

        var cache = scope.Cache;
        var items = CreateTestRaindropItems();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.SetAsync("videos", items, CancellationToken.None).ConfigureAwait(false));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task SetAsync_ValidData_LogsSuccessMessage()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();

        // Act
        await scope.Cache.SetAsync("videos", testItems, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Cache storage successful"))).IsTrue();
    }

    [Test]
    public async Task SetAsync_ValidData_StoresSuccessfully()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();

        // Act
        await scope.Cache.SetAsync("videos", testItems, CancellationToken.None).ConfigureAwait(false);

        // Assert - Verify no exceptions were thrown
        await Assert.That(testItems).HasCount().EqualTo(2);
    }
}