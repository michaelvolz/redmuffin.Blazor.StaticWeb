using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

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

        // Check if error message appears in markup
        // Check for the exact error message
        var hasExactErrorMessage = component.Markup.Contains("Unable to refresh articles. Please check your internet connection and try again.");

        // Check for various error-related content
        var hasCalloutAlert = component.Markup.Contains("callout alert");
        var hasUnableToRefresh = component.Markup.Contains("Unable to refresh");
        var hasErrorKeyword = component.Markup.Contains("error") || component.Markup.Contains("Error");

        // Check if the error message div structure exists
        var hasErrorDiv = component.Markup.Contains("div class=\"callout alert\"");

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
}