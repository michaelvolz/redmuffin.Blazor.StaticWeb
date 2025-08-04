using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Pages.VideosPage;

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
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
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

        // Wait for component initialization and background refresh
        component.WaitForState(() =>
        {
            var badges = component.FindAll(".refresh-badge");
            if (badges.Count == 0) return false;
            return badges[0].GetAttribute("class")?.Contains("refresh-badge--visible") == true;
        }, TimeSpan.FromSeconds(2));

        // Verify refresh badge is visible
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");

        // Act - Click refresh badge
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Wait for refresh operation to complete by checking badge state
        component.WaitForState(() =>
        {
            var badge = component.Find(".refresh-badge");
            var classes = badge.GetAttribute("class");
            return classes != null && !classes.Contains("refresh-badge--loading");
        }, TimeSpan.FromSeconds(2));

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
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Allow refresh to complete

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).HasCount().EqualTo(2);
            await Assert.That(component.Markup).Contains("Updated Video");
            await Assert.That(component.Markup).Contains("New Video");
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
        }
    }
}