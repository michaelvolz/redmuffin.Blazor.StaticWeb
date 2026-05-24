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