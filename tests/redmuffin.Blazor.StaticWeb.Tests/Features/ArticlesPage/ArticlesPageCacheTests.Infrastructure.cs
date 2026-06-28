using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_MultipleRefreshClicks_PreventsDoubleRefresh()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Old Article", "Old excerpt")
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Updated Article", "Updated excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);
        scope.RaindropAPI_Mock.SetupDelay(300); // Add delay to test multiple clicks

        var component = scope.Context.Render<Articles>();

        // Await background refresh completion deterministically — the mock has SetupDelay(300)
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Act
        // Wait for background refresh to show badge (data differs)
        component.WaitForElement(".refresh-badge", TimeSpan.FromSeconds(5));

        // First click — re-find badge in case background refresh caused re-render
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Second click should be ignored — find fresh after any re-renders
        var badgesAfterClick = component.FindAll(".refresh-badge");
        if (badgesAfterClick.Count > 0)
        {
            await badgesAfterClick[0].ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        }

        // Wait for refresh to complete — badge goes back to Hidden after success
        component.WaitForState(() =>
        {
            var badges = component.FindAll(".refresh-badge");
            return badges.Count == 0;
        }, TimeSpan.FromSeconds(5));

        // Assert — badge no longer in DOM (Hidden)
        await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
    }

    [Test]
    public async Task ArticlesPage_NoCachedData_FetchesFreshDataImmediately()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Fresh Article 1", "Fresh excerpt 1"),
            CreateTestArticle("2", "Fresh Article 2", "Fresh excerpt 2")
        };

        scope.CacheService_Mock.SetupNoCachedData("Articles");
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Fresh Article 1");
            await Assert.That(component.Markup).Contains("Fresh Article 2");
            // No refresh badge should be visible since we loaded fresh data immediately
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
        }
    }

    [Test]
    public async Task ArticlesPage_OnInitialization_LoadsCachedDataFirst()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Cached Article 1", "Cached excerpt 1"),
            CreateTestArticle("2", "Cached Article 2", "Cached excerpt 2")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPI_Mock.SetupArticles(new List<RaindropItem>());

        // Act
        var component = scope.Context.Render<Articles>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Cached Article 1");
            await Assert.That(component.Markup).Contains("Cached Article 2");
        }
    }

    [Test]
    public async Task ArticlesPage_WhenFreshDataDiffers_ShowsRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Old Article", "Old excerpt")
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Updated Article", "Updated excerpt"),
            CreateTestArticle("2", "New Article", "New excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();

        // Await background refresh completion deterministically — zero polling, zero delay
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");
    }
}