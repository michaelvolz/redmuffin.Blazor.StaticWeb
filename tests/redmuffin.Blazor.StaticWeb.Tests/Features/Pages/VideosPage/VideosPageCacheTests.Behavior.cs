using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.VideosPage;

[Category("Feature:Videos")]
public sealed partial class VideosPageCacheTests
{
    [Test]
    public async Task VideosPage_OnInitialization_LoadsCachedDataFirst()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Cached Video 1", "Cached excerpt 1"),
            CreateTestVideo("2", "Cached Video 2", "Cached excerpt 2")
        };

        scope.CacheService_Mock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPI_Mock.SetupVideos(new List<RaindropItem>());

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(50).ConfigureAwait(false); // Allow component to initialize

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Cached Video 1");
            await Assert.That(component.Markup).Contains("Cached Video 2");
        }
    }

    [Test]
    public async Task VideosPage_WhenFreshDataDiffers_ShowsRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Old Video", "Old excerpt")
        };
        var freshVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Updated Video", "Updated excerpt"),
            CreateTestVideo("2", "New Video", "New excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPI_Mock.SetupVideos(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");
    }
}