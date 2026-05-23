using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_CacheFailure_FallsBackToFreshData()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Fallback Article", "Fallback excerpt")
        };

        scope.CacheService_Mock.SetupCacheFailure("Articles");
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(1);
            await Assert.That(component.Markup).Contains("Fallback Article");
        }
    }

    [Test]
    public async Task ArticlesPage_RefreshFailure_ShowsErrorState()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Cached Article", "Cached excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPI_Mock.SetupFailure("Network error");

        var component = scope.Context.Render<Articles>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Wait for error state to be set with longer timeout
        component.WaitForState(() =>
        {
            var badge = component.Find(".refresh-badge");
            var classes = badge.GetAttribute("class");
            return classes != null && classes.Contains("refresh-badge--error");
        }, TimeSpan.FromSeconds(10));

        // Wait a bit more for the error message to appear in markup
        await Task.Delay(500).ConfigureAwait(false);

        // Assert - Check badge error state first
        var finalBadge = component.Find(".refresh-badge");
        await Assert.That(finalBadge.GetAttribute("class")).Contains("refresh-badge--error");

        await Assert.That(component.Markup)
            .Contains("Unable to refresh. Please check your internet connection and try again.");
    }
}