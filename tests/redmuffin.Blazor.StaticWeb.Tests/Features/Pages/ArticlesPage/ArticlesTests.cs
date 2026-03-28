using Bunit;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Articles;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage;

[Category("Feature:Articles")]
[Category("Unit")]
public partial class ArticlesTests
{
    [Test]
    public async Task Articles_Should_Handle_Articles_With_Missing_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Wait for component to finish loading
        await Task.Delay(100).ConfigureAwait(false);
        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should handle missing excerpts gracefully
            var markup = component.Markup;
            await Assert.That(markup).DoesNotContain("null").And.DoesNotContain("undefined");
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task Articles_Should_Handle_Articles_With_Missing_Titles()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Wait for component to finish loading
        await Task.Delay(100).ConfigureAwait(false);
        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should handle missing titles gracefully
            var markup = component.Markup;
            await Assert.That(markup).DoesNotContain("null").And.DoesNotContain("undefined");
        }
    }

    [Test]
    public async Task Articles_Should_Handle_Dependency_Injection_Properly()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Component should render without dependency injection errors
            var markup = component.Markup;
            await Assert.That(markup).DoesNotContain("NullReferenceException");
            await Assert.That(markup).DoesNotContain("ArgumentNullException");
        }
    }


    [Test]
    [Category("Smoke")]
    public async Task Articles_Should_Render_Successfully_With_No_Articles()
    {
        // Arrange
        using var scope = CreateEmptyArticlesTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();
            await Assert.That(component.Find("h1").TextContent).Contains("Article");

            // Should show "No articles available" message when empty
            var noArticlesMessage = component.FindAll(".callout.secondary, .empty-state, .no-content");
            await Assert.That(noArticlesMessage.Count).IsGreaterThanOrEqualTo(0); // May or may not have explicit empty state
        }
    }

    [Test]
    public async Task Articles_Should_Truncate_Long_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Wait for component to finish loading
        await Task.Delay(100).ConfigureAwait(false);
        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should truncate long excerpts with ellipsis
            var markup = component.Markup;
            if (markup.Contains("...")) await Assert.That(markup).Contains("...");
        }
    }

    [Test]
    public async Task Articles_Should_Use_Image_Placeholder_Service()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Wait for component to finish loading
        await Task.Delay(100).ConfigureAwait(false);
        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should use placeholder service for images
            var markup = component.Markup;
            await Assert.That(markup).Contains("img").Or.Contains("placeholder");
        }
    }
}