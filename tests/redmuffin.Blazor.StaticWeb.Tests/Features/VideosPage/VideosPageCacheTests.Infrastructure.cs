using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.VideosPage;

[Category("Feature:Videos")]
public sealed partial class VideosPageCacheTests
{
    [Test]
    public async Task VideosPage_IdenticalFreshData_DoesNotShowRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var identicalVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Same Video", "Same excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Videos", identicalVideos);
        scope.RaindropAPI_Mock.SetupVideos(identicalVideos); // Same data

        // Act
        var component = scope.Context.Render<Videos>();

        // Assert
        await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
    }

    [Test]
    public async Task VideosPage_RefreshBadgeClick_CallsApiAndShowsBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Old Video", "Old excerpt")
        };
        var freshVideos = new List<RaindropItem>
        {
            CreateTestVideo("2", "New Video", "New excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPI_Mock.SetupVideos(freshVideos);
        // No artificial delay - let the component handle its own timing

        var component = scope.Context.Render<Videos>();

        // Await background refresh completion deterministically — zero polling, zero delay
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Verify refresh badge is visible
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");

        // Act - Click refresh badge
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify no error state
        await Assert.That(component.Markup).DoesNotContain("refresh-badge--error");
    }

    [Test]
    public async Task VideosPage_RefreshBadgeClick_UpdatesDataAndHidesBadge()
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

        var component = scope.Context.Render<Videos>();

        // Await background refresh completion deterministically — zero polling, zero delay
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Updated Video");
            await Assert.That(component.Markup).Contains("New Video");
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
        }
    }
}