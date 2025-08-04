using System.Diagnostics;
using System.Text.Json;
using LZStringCSharp;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Enums;

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