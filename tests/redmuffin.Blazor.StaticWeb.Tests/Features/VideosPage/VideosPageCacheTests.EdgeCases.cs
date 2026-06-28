using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.VideosPage;

[Category("Feature:Videos")]
public sealed partial class VideosPageCacheTests
{
    [Test]
    public async Task VideosPage_CacheFailure_FallsBackToFreshData()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Fallback Video", "Fallback excerpt")
        };

        scope.CacheService_Mock.SetupCacheFailure("Videos");
        scope.RaindropAPI_Mock.SetupVideos(freshVideos);

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

        scope.CacheService_Mock.SetupNoCachedData("Videos");
        scope.RaindropAPI_Mock.SetupVideos(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();

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