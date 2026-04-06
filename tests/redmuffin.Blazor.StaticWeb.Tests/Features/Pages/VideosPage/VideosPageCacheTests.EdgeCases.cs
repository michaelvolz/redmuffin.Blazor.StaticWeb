using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.VideosPage;

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
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

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
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Fresh Video 1");
            await Assert.That(component.Markup).Contains("Fresh Video 2");
            // No refresh badge should be visible since we loaded fresh data immediately
            var refreshBadge = component.Find(".refresh-badge");
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
        }
    }

    [Test]
    public async Task VideosPage_RefreshFailure_ShowsErrorState()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Cached Video", "Cached excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPI_Mock.SetupFailure("Network error");

        var component = scope.Context.Render<Videos>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Wait for error state to be set
        component.WaitForState(() =>
        {
            var badge = component.Find(".refresh-badge");
            var classes = badge.GetAttribute("class");
            return classes != null && classes.Contains("refresh-badge--error");
        }, TimeSpan.FromSeconds(2));

        // Debug: Check what's actually in the markup
        Console.WriteLine($"Component markup after error: {component.Markup}");

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--error");
            await Assert.That(component.Markup).Contains("Unable to refresh videos. Please check your internet connection and try again.");
        }
    }
}