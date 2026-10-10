using Bunit;
using redmuffin.Blazor.StaticWeb.Components.Raindrop;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Pages.Articles.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

[Category("Feature:Articles")]
[Category("Unit")]
public partial class ArticlesTests
{
    [Test]
    public async Task Articles_Should_Handle_Articles_With_Missing_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();
        var emptyExcerpt = CreateTestItem(id: 1, excerpt: "");
        var nullExcerpt = CreateTestItem(id: 2, excerpt: null!);
        scope.Mediator_Mock.SetupLoad([emptyExcerpt, nullExcerpt]);
        scope.Mediator_Mock.SetupRefresh([emptyExcerpt, nullExcerpt]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.Markup).Contains("No Excerpt Available");
            await Assert
                .That(component.Markup)
                .DoesNotContain("null")
                .And.DoesNotContain("undefined");
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task Articles_Should_Handle_Articles_With_Missing_Titles()
    {
        // Arrange
        using var scope = CreateTestScope();
        var emptyTitle = CreateTestItem(id: 1, title: "");
        var nullTitle = CreateTestItem(id: 2, title: null!);
        scope.Mediator_Mock.SetupLoad([emptyTitle, nullTitle]);
        scope.Mediator_Mock.SetupRefresh([emptyTitle, nullTitle]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.Markup).Contains("No Title Available");
            await Assert
                .That(component.Markup)
                .DoesNotContain("null")
                .And.DoesNotContain("undefined");
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

        // Assert — empty state container is visible
        await Assert.That(component.Find($"#{RaindropItemList.EmptyStateElementId}")).IsNotNull();
    }

    [Test]
    public async Task Articles_Should_Truncate_Long_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();
        var longExcerpt = new string('A', 300);
        var item = CreateTestItem(excerpt: longExcerpt);
        scope.Mediator_Mock.SetupLoad([item]);
        scope.Mediator_Mock.SetupRefresh([item]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        await Assert.That(component.Markup).Contains("...");
        await Assert.That(component.Markup).DoesNotContain(longExcerpt);
    }

    [Test]
    public async Task Articles_Should_Use_Image_Placeholder_Service()
    {
        // Arrange
        using var scope = CreateTestScope();
        var withCover = CreateTestItem(
            id: 1,
            link: "https://example.com/with-cover",
            cover: "https://example.com/cover.jpg"
        );
        var withoutCover = CreateTestItem(
            id: 2,
            link: "https://example.com/without-cover",
            cover: null!
        );
        scope.Mediator_Mock.SetupLoad([withCover, withoutCover]);
        scope.Mediator_Mock.SetupRefresh([withCover, withoutCover]);

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        // Assert
        var images = component.FindAll("img");
        await Assert.That(images).Count().IsEqualTo(2);
        await Assert.That(images[0].GetAttribute("src")).IsEqualTo("https://example.com/cover.jpg");
        await Assert.That(images[1].GetAttribute("src")).IsEqualTo("/images/placeholder.svg");
    }
}
