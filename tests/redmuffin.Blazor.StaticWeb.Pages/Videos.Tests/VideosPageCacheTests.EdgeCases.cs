using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Videos;

namespace redmuffin.Blazor.StaticWeb.Pages.Videos.Tests;

[Category("Feature:Videos")]
public sealed partial class VideosPageCacheTests
{
    [Test]
    public async Task VideosPage_CacheFailure_FallsBackToFreshData()
    {
        // Arrange — Load handler falls back to API on cache failure; page only sees successful Load.
        using var scope = CreateTestScope();
        var freshVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Fallback Video", "Fallback excerpt")
        };

        scope.Mediator_Mock.SetupLoad(freshVideos, isFromCache: false);
        scope.Mediator_Mock.SetupRefresh(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(1);
            await Assert.That(component.Markup).Contains("Fallback Video");
        }
    }

    [Test]
    public async Task VideosPage_NoCachedData_FetchesFreshDataImmediately()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Fresh Video 1", "Fresh excerpt 1"),
            CreateTestVideo("2", "Fresh Video 2", "Fresh excerpt 2")
        };

        scope.Mediator_Mock.SetupLoad(freshVideos, isFromCache: false);
        scope.Mediator_Mock.SetupRefresh(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();

        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Fresh Video 1");
            await Assert.That(component.Markup).Contains("Fresh Video 2");
            // No refresh badge should be visible since we loaded fresh data immediately
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
        }
    }
}
