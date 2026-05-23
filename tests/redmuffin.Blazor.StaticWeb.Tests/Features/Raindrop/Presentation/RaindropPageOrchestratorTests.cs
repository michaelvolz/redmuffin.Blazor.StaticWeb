using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Enums;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Presentation;

public sealed partial class RaindropPageOrchestratorTests
{
    [Test]
    public async Task LoadCachedDataAsync_ShouldPopulateItems_WhenCacheHit()
    {
        // Arrange
        var ctx = new RaindropPageContext();
        var cache = new RaindropItemsCache_Fake
        {
            GetResult = new RaindropCacheResult<IList<RaindropItem>>
            {
                Status = RaindropCacheStatus.Hit,
                Data = [new RaindropItem { Title = "Test Video", Link = "https://example.com" }]
            }
        };
        var fetchCalled = false;
        var imagesPopulated = false;
        var logger = new Logger_Spy();

        // Act
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            ctx,
            "Videos",
            cache,
            _ => { fetchCalled = true; return Task.FromResult(Enumerable.Empty<RaindropItem>()); },
            () => { imagesPopulated = true; return Task.CompletedTask; },
            logger).ConfigureAwait(false);

        // Assert — behavior locked from both pages' LoadCachedDataAsync
        await Assert.That(ctx.Items).IsNotNull();
        await Assert.That(ctx.Items!.Count).IsEqualTo(1);
        await Assert.That(fetchCalled).IsFalse(); // cache hit, no fetch
        await Assert.That(imagesPopulated).IsTrue(); // image cache populated
    }

    [Test]
    public async Task LoadCachedDataAsync_ShouldFetch_WhenCacheMiss()
    {
        // Arrange
        var ctx = new RaindropPageContext();
        var cache = new RaindropItemsCache_Fake
        {
            GetResult = new RaindropCacheResult<IList<RaindropItem>>
            {
                Status = RaindropCacheStatus.Miss
            }
        };
        var items = new List<RaindropItem> { new() { Title = "Fresh Item", Link = "https://example.com" } };
        var fetchCalled = false;
        var imagesPopulated = false;
        var logger = new Logger_Spy();

        // Act
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            ctx,
            "Videos",
            cache,
            _ => { fetchCalled = true; return Task.FromResult<IEnumerable<RaindropItem>>(items); },
            () => { imagesPopulated = true; return Task.CompletedTask; },
            logger).ConfigureAwait(false);

        // Assert — cache miss triggers fetch
        await Assert.That(fetchCalled).IsTrue();
        await Assert.That(ctx.Items).IsNotNull();
        await Assert.That(ctx.Items!.Count).IsEqualTo(1);
        await Assert.That(imagesPopulated).IsTrue();
    }
}
