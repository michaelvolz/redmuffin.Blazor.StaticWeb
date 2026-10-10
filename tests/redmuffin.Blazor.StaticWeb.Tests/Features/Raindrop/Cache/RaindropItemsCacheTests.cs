using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Enums;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Cache;

[Category("Feature:Cache")]
[Category("Unit")]
public partial class RaindropItemsCacheTests
{
    [Test]
    [Category("Smoke")]
    public async Task SetAsync_Then_GetAsync_Returns_Stored_Items()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = CreateTestRaindropItems();

        // Act
        await scope
            .Cache.SetAsync("videos", testItems, CancellationToken.None)
            .ConfigureAwait(false);
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).Count().IsEqualTo(2);
            await Assert.That(result.Data![0].Title).IsEqualTo(testItems[0].Title);
        }
    }

    [Test]
    [Category("Smoke")]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Cache_RoundTrip_Preserves_Pruned_Item_Fields(bool useLargeDataSet)
    {
        // Arrange
        using var scope = CreateTestScope();
        var testItems = useLargeDataSet ? CreateLargeTestDataSet() : CreateTestRaindropItems();

        // Act
        await scope
            .Cache.SetAsync("videos", testItems, CancellationToken.None)
            .ConfigureAwait(false);
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).Count().IsEqualTo(testItems.Count);
        }

        var storedItems = result.Data!;
        for (var i = 0; i < storedItems.Count; i++)
        {
            using (Assert.Multiple())
            {
                await Assert.That(storedItems[i].Id).IsEqualTo(testItems[i].Id);
                await Assert.That(storedItems[i].Link).IsEqualTo(testItems[i].Link);
                await Assert.That(storedItems[i].Title).IsEqualTo(testItems[i].Title);
                await Assert.That(storedItems[i].Excerpt).IsEqualTo(testItems[i].Excerpt);
                await Assert.That(storedItems[i].Cover).IsEqualTo(testItems[i].Cover);
            }
        }

        await Assert
            .That(
                scope.LocalStorageService_Mock.SetKeys.Count(key => key == "raindrop_cache_videos")
            )
            .IsEqualTo(1);
    }

    [Test]
    public async Task GetAsync_LargeDataSet_ReturnsHit_With_All_Items()
    {
        // Arrange
        using var scope = CreateTestScope();
        var largeDataSet = CreatePerformanceTestDataSet();
        await scope
            .Cache.SetAsync("videos", largeDataSet, CancellationToken.None)
            .ConfigureAwait(false);

        // Act
        var result = await scope
            .Cache.GetAsync("videos", CancellationToken.None)
            .ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result.Status).IsEqualTo(RaindropCacheStatus.Hit);
            await Assert.That(result.Data).Count().IsEqualTo(largeDataSet.Count);
        }
    }
}
