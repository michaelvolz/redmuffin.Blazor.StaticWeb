using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Pages.ArticlesPage;

/// <summary>
///     Integration tests for Articles page caching functionality.
///     Tests the integration between Articles component, RefreshBadge, and caching services.
/// </summary>
public partial class ArticlesPageCacheTests
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

        scope.CacheServiceMock.SetupCacheFailure("Articles");
        scope.RaindropAPIMock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).HasCount().EqualTo(1);
            await Assert.That(component.Markup).Contains("Fallback Article");
        }
    }

    [Test]
    public async Task ArticlesPage_IdenticalFreshData_DoesNotShowRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var identicalArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Same Article", "Same excerpt")
        };

        scope.CacheServiceMock.SetupCachedData("Articles", identicalArticles);
        scope.RaindropAPIMock.SetupArticles(identicalArticles); // Same data

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
    }

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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupArticles(freshArticles);
        scope.RaindropAPIMock.SetupDelay(300); // Add delay to test multiple clicks

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

        scope.CacheServiceMock.SetupNoCachedData("Articles");
        scope.RaindropAPIMock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to load

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).HasCount().EqualTo(2);
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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupArticles(new List<RaindropItem>());

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(50).ConfigureAwait(false); // Allow component to initialize

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).HasCount().EqualTo(2);
            await Assert.That(component.Markup).Contains("Cached Article 1");
            await Assert.That(component.Markup).Contains("Cached Article 2");
        }
    }

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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupArticles(freshArticles);
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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupArticles(freshArticles);

        var component = scope.Context.Render<Articles>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to show badge

        // Act
        var refreshBadge = component.Find(".refresh-badge");
        await refreshBadge.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Allow refresh to complete

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).HasCount().EqualTo(2);
            await Assert.That(component.Markup).Contains("Updated Article");
            await Assert.That(component.Markup).Contains("New Article");
            await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--hidden");
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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupFailure("Network error");

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

        // Check if error message appears in markup
        // Check for the exact error message
        var hasExactErrorMessage = component.Markup.Contains("Unable to refresh articles. Please check your internet connection and try again.");

        // Check for various error-related content
        var hasCalloutAlert = component.Markup.Contains("callout alert");
        var hasUnableToRefresh = component.Markup.Contains("Unable to refresh");
        var hasErrorKeyword = component.Markup.Contains("error") || component.Markup.Contains("Error");

        // Check if the error message div structure exists
        var hasErrorDiv = component.Markup.Contains("<div class=\"callout alert\">");

        if (!hasExactErrorMessage)
        {
            // Extract a portion of the markup around any error text for debugging
            var markupSnippet = "";
            if (hasUnableToRefresh)
            {
                var index = component.Markup.IndexOf("Unable to refresh", StringComparison.OrdinalIgnoreCase);
                var start = Math.Max(0, index - 50);
                var length = Math.Min(200, component.Markup.Length - start);
                markupSnippet = component.Markup.Substring(start, length);
            }

            Assert.Fail(
                $"Error message not found. Has callout alert: {hasCalloutAlert}, Has error div: {hasErrorDiv}, Has 'Unable to refresh': {hasUnableToRefresh}, Has error keyword: {hasErrorKeyword}. Markup snippet: '{markupSnippet}'");
        }

        await Assert.That(hasExactErrorMessage).IsTrue();
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

        scope.CacheServiceMock.SetupCachedData("Articles", cachedArticles);
        scope.RaindropAPIMock.SetupArticles(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();
        await Task.Delay(200).ConfigureAwait(false); // Allow background refresh to complete

        // Assert
        var refreshBadge = component.Find(".refresh-badge");
        await Assert.That(refreshBadge.GetAttribute("class")).Contains("refresh-badge--visible");
    }
}