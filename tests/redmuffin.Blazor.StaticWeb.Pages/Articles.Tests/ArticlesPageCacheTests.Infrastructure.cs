using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

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
            CreateTestArticle("1", "Old Article", "Old excerpt"),
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Updated Article", "Updated excerpt"),
        };

        scope.Mediator_Mock.SetupLoad(cachedArticles, isFromCache: true);
        scope.Mediator_Mock.SetupRefresh(freshArticles);

        var component = scope.Context.Render<Articles>();

        // Await background refresh completion deterministically
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        var refreshCallsBeforeClick = scope.Mediator_Mock.RefreshCallCount;

        // Act — first click refreshes once and hides the badge
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        var refreshCallsAfterFirstClick = scope.Mediator_Mock.RefreshCallCount;

        // Second click attempt is a no-op because the badge is already hidden
        var badgesAfterClick = component.FindAll(".refresh-badge");
        if (badgesAfterClick.Count > 0)
            await badgesAfterClick[0].ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert — exactly one refresh per click, and no refresh from the second click
        using (Assert.Multiple())
        {
            await Assert.That(refreshCallsAfterFirstClick).IsEqualTo(refreshCallsBeforeClick + 1);
            await Assert
                .That(scope.Mediator_Mock.RefreshCallCount)
                .IsEqualTo(refreshCallsAfterFirstClick);
            await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
        }
    }

    [Test]
    public async Task ArticlesPage_NoCachedData_FetchesFreshDataImmediately()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Fresh Article 1", "Fresh excerpt 1"),
            CreateTestArticle("2", "Fresh Article 2", "Fresh excerpt 2"),
        };

        scope.Mediator_Mock.SetupLoad(freshArticles, isFromCache: false);
        scope.Mediator_Mock.SetupRefresh(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();

        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

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
            CreateTestArticle("2", "Cached Article 2", "Cached excerpt 2"),
        };

        scope.Mediator_Mock.SetupLoad(cachedArticles, isFromCache: true);
        // Empty refresh keeps cached items on screen (badge may show — not under test here)
        scope.Mediator_Mock.SetupRefresh([]);

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
            CreateTestArticle("1", "Old Article", "Old excerpt"),
        };
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Updated Article", "Updated excerpt"),
            CreateTestArticle("2", "New Article", "New excerpt"),
        };

        scope.Mediator_Mock.SetupLoad(cachedArticles, isFromCache: true);
        scope.Mediator_Mock.SetupRefresh(freshArticles);

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
