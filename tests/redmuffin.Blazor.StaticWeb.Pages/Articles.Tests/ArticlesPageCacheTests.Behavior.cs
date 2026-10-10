using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_RefreshBadgeClick_UpdatesDataAndHidesBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var cachedArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Old Article", "Old excerpt"),
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Updated Article", "Updated excerpt"),
            CreateTestArticle("2", "New Article", "New excerpt"),
        };

        scope.Mediator_Mock.SetupLoad(cachedArticles, isFromCache: true);
        scope.Mediator_Mock.SetupRefresh(freshArticles);

        var component = scope.Context.Render<Articles>();

        // Await background refresh completion deterministically — zero polling, zero delay
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Verify refresh badge is visible
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");

        var refreshCallsBeforeClick = scope.Mediator_Mock.RefreshCallCount;

        // Act - Click refresh badge
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert
                .That(scope.Mediator_Mock.RefreshCallCount)
                .IsEqualTo(refreshCallsBeforeClick + 1);
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(2);
            await Assert.That(component.Markup).Contains("Updated Article");
            await Assert.That(component.Markup).Contains("New Article");
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
            await Assert.That(component.Markup).DoesNotContain("refresh-badge--error");
        }
    }
}
