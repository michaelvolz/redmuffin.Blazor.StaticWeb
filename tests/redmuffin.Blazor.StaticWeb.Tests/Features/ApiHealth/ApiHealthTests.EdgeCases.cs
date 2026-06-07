using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ApiHealth;

[Category("Feature:ApiHealth")]
public sealed partial class ApiHealthTests
{
    [Test]
    public async Task Displays_error_message_when_mediator_fails()
    {
        // Arrange
        using var scope = CreateFailingTestScope();
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Features.ApiHealth.ApiHealth>();
        var button = component.Find("button:contains('Call ApiHealth')");

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        var errorElement = component.Find("p:contains('Error:')");
        await Assert.That(errorElement).IsNotNull();
        await Assert.That(errorElement.TextContent).Contains("Error calling API");
    }
}
