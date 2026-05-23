using Bunit;
using ArticlesComponent = redmuffin.Blazor.StaticWeb.Features.ArticlesPage.Articles;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

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

        // Wait for initial state
        await Task.Delay(50).ConfigureAwait(false);
        var initialMarkup = component.Markup;

        // Wait for loading to complete
        await Task.Delay(100).ConfigureAwait(false);
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