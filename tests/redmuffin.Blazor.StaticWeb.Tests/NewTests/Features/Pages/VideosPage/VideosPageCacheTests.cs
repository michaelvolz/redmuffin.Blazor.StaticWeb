using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Pages.VideosPage;

/// <summary>
///     Integration tests for Videos page caching functionality.
///     Tests the integration between Videos component, RefreshBadge, and caching services.
/// </summary>
public partial class VideosPageCacheTests
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

        scope.CacheServiceMock.SetupCacheFailure("Videos");
        scope.RaindropAPIMock.SetupVideos(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).HasCount().EqualTo(1);
            await Assert.That(component.Markup).Contains("Fallback Video");
        }
    }

    [Test]
    public async Task VideosPage_IdenticalFreshData_DoesNotShowRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var identicalVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Same Video", "Same excerpt")
        };

        scope.CacheServiceMock.SetupCachedData("Videos", identicalVideos);
        scope.RaindropAPIMock.SetupVideos(identicalVideos); // Same data

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
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

        scope.CacheServiceMock.SetupNoCachedData("Videos");
        scope.RaindropAPIMock.SetupVideos(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).HasCount().EqualTo(2);
            await Assert.That(component.Markup).Contains("Fresh Video 1");
            await Assert.That(component.Markup).Contains("Fresh Video 2");
            // No refresh badge should be visible since we loaded fresh data immediately
            var refreshBadge = component.Find(".refresh-badge");
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
        }
    }

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

        scope.CacheServiceMock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPIMock.SetupVideos(new List<RaindropItem>());

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(50).ConfigureAwait(false); // Allow component to initialize

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".video-card")).HasCount().EqualTo(2);
            await Assert.That(component.Markup).Contains("Cached Video 1");
            await Assert.That(component.Markup).Contains("Cached Video 2");
        }
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

        scope.CacheServiceMock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPIMock.SetupVideos(freshVideos);
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

        scope.CacheServiceMock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPIMock.SetupVideos(freshVideos);

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

    [Test]
    public async Task VideosPage_RefreshFailure_ShowsErrorState()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Cached Video", "Cached excerpt")
        };

        scope.CacheServiceMock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPIMock.SetupFailure("Network error");

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

        scope.CacheServiceMock.SetupCachedData("Videos", cachedVideos);
        scope.RaindropAPIMock.SetupVideos(freshVideos);

        // Act
        var component = scope.Context.Render<Videos>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");
    }
}