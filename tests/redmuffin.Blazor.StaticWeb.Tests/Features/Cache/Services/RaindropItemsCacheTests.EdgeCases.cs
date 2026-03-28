using System.Text.Json;
using LZStringCSharp;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Enums;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Cache.Services;

[Category("Feature:Cache")]
public sealed partial class RaindropItemsCacheTests
{
    [Test]
    [Category("Smoke")]
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
        scope.LocalStorageService_Mock.SetupRemoveItemAsyncThrows("raindrop_cache_videos", new InvalidOperationException("Storage access denied"));

        var cache = scope.Cache;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.ClearAsync("videos").ConfigureAwait(false));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    [Category("Smoke")]
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
            await Assert.That(deserializedItems).Count().IsEqualTo(0);
        }
    }

    [Test]
    public async Task GetAsync_DecompressionFailure_ReturnsError()
    {
        // Arrange
        using var scope = CreateTestScope();
        var invalidCompressedData = "invalid_compressed_data";
        var metadata = CreateTestMetadata();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageService_Mock.SetupGetItemAsync("raindrop_cache_videos", invalidCompressedData);

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
    [Category("Smoke")]
    public async Task GetAsync_EmptyCache_ReturnsNotFound()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", false);

        // Act
        var result = await scope.Cache.GetAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Miss);
    }

    [Test]
    public async Task GetAsync_LocalStorageDataRetrievalThrows_ReturnsError()
    {
        // Arrange
        using var scope = CreateTestScope();
        var metadata = CreateTestMetadata();
        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync("raindrop_cache_videos_metadata", metadata);
        scope.LocalStorageService_Mock.SetupGetItemAsyncThrows<string>("raindrop_cache_videos", new InvalidOperationException("Storage access denied"));

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
        scope.LocalStorageService_Mock.SetupContainKeyAsyncThrows("raindrop_cache_videos_metadata", new InvalidOperationException("LocalStorage unavailable"));

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
        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
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
    public async Task IsExpiredAsync_CorruptedMetadata_ReturnsTrue()
    {
        // Arrange
        using var scope = CreateTestScope();

        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsync<RaindropCacheMetadata>("raindrop_cache_videos_metadata", null);

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
        scope.LocalStorageService_Mock.SetupContainKeyAsyncThrows("raindrop_cache_videos_metadata", new InvalidOperationException("Storage unavailable"));

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
        scope.LocalStorageService_Mock.SetupContainKeyAsync("raindrop_cache_videos_metadata", true);
        scope.LocalStorageService_Mock.SetupGetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
            new InvalidOperationException("Storage corrupted"));

        // Act
        var result = await scope.Cache.IsExpiredAsync("videos", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task SetAsync_LocalStorageMetadataSetThrows_LogsWarningButContinues()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();
        scope.LocalStorageService_Mock.SetupSetItemAsyncThrows<RaindropCacheMetadata>("raindrop_cache_videos_metadata",
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
        scope.LocalStorageService_Mock.SetupSetItemAsyncThrows<string>("raindrop_cache_videos", new InvalidOperationException("Storage quota exceeded"));

        var cache = scope.Cache;
        var items = CreateTestRaindropItems();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.SetAsync("videos", items, CancellationToken.None).ConfigureAwait(false));
        await Assert.That(exception).IsNotNull();
    }
}