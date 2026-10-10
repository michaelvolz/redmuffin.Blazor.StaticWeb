using Bunit;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Pages.Articles.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

[Category("Feature:Articles")]
public sealed partial class ArticlesTests
{
    [Test]
    [Category("Smoke")]
    public async Task Articles_Should_Display_Articles_When_Available()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should display article titles
            var articleElements = component.FindAll(
                ".article-item, .card, [data-testid='article']"
            );
            if (articleElements.Count > 0)
                await Assert.That(articleElements.Count).IsGreaterThan(0);

            // Check for article content in the rendered markup
            var markup = component.Markup;
            await Assert.That(markup).Contains("Test Article"); // From our mock data
        }
    }

    [Test]
    public async Task Articles_Should_Display_Fallback_For_Missing_Images()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem(link: "https://example.com/missing-cover", cover: null!);
        scope.Mediator_Mock.SetupLoad([item]);
        scope.Mediator_Mock.SetupRefresh([item]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        await Assert
            .That(component.Find("img").GetAttribute("src"))
            .IsEqualTo("/images/placeholder.svg");
    }

    [Test]
    public async Task Articles_Should_Populate_Image_Cache_On_Load()
    {
        // Arrange
        using var scope = CreateTestScope();
        var item = CreateTestItem(
            link: "https://example.com/loaded-image",
            cover: "https://example.com/cover.jpg"
        );
        scope.Mediator_Mock.SetupLoad([item]);
        scope.Mediator_Mock.SetupRefresh([item]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Await background refresh so re-render does not invalidate the img event handler
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        await component
            .Find("img")
            .TriggerEventAsync("onload", EventArgs.Empty)
            .ConfigureAwait(false);

        // Assert
        var loadCall = scope.ImagePlaceholderService.HandleImageLoadCalls.Single();
        using (Assert.Multiple())
        {
            await Assert.That(loadCall.ItemLink).IsEqualTo(item.Link);
            await Assert.That(loadCall.ImageUrlCache[item.Link!]).IsEqualTo(item.Cover!);
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(1);
            await Assert.That(scope.ImageUrlResolver.PopulateCallCount).IsEqualTo(1);
        }
    }
}
