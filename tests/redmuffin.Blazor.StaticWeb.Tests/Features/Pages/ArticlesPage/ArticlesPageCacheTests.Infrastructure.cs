using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

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
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false); // Second click should be ignored
        await Task.Delay(50).ConfigureAwait(false);

        // Assert
        // Should still be in loading state, not processing second click
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--loading");
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
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Fresh Article 1");
            await Assert.That(component.Markup).Contains("Fresh Article 2");
            // No refresh badge should be visible since we loaded fresh data immediately
            var refreshBadge = component.Find(".refresh-badge");
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
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
        await Task.Delay(50).ConfigureAwait(false); // Allow component to initialize

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
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");
    }
}