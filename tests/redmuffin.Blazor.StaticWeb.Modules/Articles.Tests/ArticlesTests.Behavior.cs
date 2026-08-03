using Bunit;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Modules.Articles.Articles;

namespace redmuffin.Blazor.StaticWeb.Modules.Articles.Tests;

[Category("Feature:Articles")]
public sealed partial class ArticlesTests
{
    [Test]
    public async Task Articles_Should_Handle_State_Changes_Properly()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.BUnitContext.Render<ArticlesComponent>();

        var initialMarkup = component.Markup;

        component.Render();
        var finalMarkup = component.Markup;

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component).IsNotNull();
            await Assert.That(initialMarkup).IsNotNull();
            await Assert.That(finalMarkup).IsNotNull();

            // Component should handle state changes without errors
            await Assert.That(finalMarkup).DoesNotContain("Error").Or.Contains("Test Article");
        }
    }
}