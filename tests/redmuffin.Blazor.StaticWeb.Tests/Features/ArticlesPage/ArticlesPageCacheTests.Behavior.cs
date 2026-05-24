using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_RefreshBadgeClick_CallsApiAndShowsBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Old Article", "Old excerpt")
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("2", "New Article", "New excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);
        // No artificial delay - let the component handle its own timing

        var component = scope.Context.Render<Articles>();

        // Wait for background refresh to show badge (different data detected)
        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(100).ConfigureAwait(false);
            component.Render(); // Force re-render to pick up async state changes
            var badges = component.FindAll(".refresh-badge");
            if (badges.Count > 0 && badges[0].GetAttribute("class")?.Contains("refresh-badge--visible") == true)
                break;
        }

        // Verify refresh badge is visible
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");

        // Act - Click refresh badge
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Wait for refresh operation to complete
        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(100).ConfigureAwait(false);
            component.Render();
            var badges = component.FindAll(".refresh-badge");
            if (badges.Count == 0) break; // badge disappeared = Hidden (refresh complete)
            var classes = badges[0].GetAttribute("class");
            if (classes != null && !classes.Contains("refresh-badge--loading")) break;
        }

        // Assert - Verify no error state
        await Assert.That(component.Markup).DoesNotContain("refresh-badge--error");
    }

    [Test]
    public async Task ArticlesPage_RefreshBadgeClick_UpdatesDataAndHidesBadge()
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

        var component = scope.Context.Render<Articles>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Allow refresh to complete

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Updated Article");
            await Assert.That(component.Markup).Contains("New Article");
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
        }
    }
}