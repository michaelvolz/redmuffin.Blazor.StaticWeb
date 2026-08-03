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
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth>();
        var button = component.Find("button.button");

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        var errorBanner = component.Find("div.callout.alert");
        await Assert.That(errorBanner).IsNotNull();
        await Assert.That(errorBanner.TextContent).Contains("Simulated mediator error");
    }
}
