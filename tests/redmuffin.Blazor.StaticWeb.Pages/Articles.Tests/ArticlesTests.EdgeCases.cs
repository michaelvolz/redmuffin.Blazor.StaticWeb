using Bunit;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Pages.Articles.Articles;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles.Tests;

[Category("Feature:Articles")]
public sealed partial class ArticlesTests
{
    [Test]
    [Category("Smoke")]
    public async Task Articles_Should_Display_Error_Message_When_API_Fails()
    {
        // Arrange
        using var scope = CreateFailingAPITestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        component.Render();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();

            // Should display error message
            var markup = component.Markup;
            await Assert.That(markup).Contains("Unable").Or.Contains("Error");
        }
    }
}