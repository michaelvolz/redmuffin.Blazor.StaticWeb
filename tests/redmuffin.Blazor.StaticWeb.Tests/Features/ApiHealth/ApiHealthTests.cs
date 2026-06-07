using Bunit;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Features.ApiHealth;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ApiHealth;

[Category("Feature:ApiHealth")]
[Category("Unit")]
public sealed partial class ApiHealthTests
{
    [Test]
    public async Task Renders_page_heading_and_button()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Features.ApiHealth.ApiHealth>();

        // Assert
        using (Assert.Multiple())
        {
            var heading = component.Find("h1");
            await Assert.That(heading).IsNotNull();
            await Assert.That(heading.TextContent).IsEqualTo("ApiHealth");

            var button = component.Find("button.button");
            await Assert.That(button).IsNotNull();
            await Assert.That(button.TextContent).Contains("Call ApiHealth");
        }
    }

    [Test]
    public async Task Displays_api_response_when_button_clicked()
    {
        // Arrange
        using var scope = CreateTestScope("Hello from handler");
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Features.ApiHealth.ApiHealth>();
        var button = component.Find("button:contains('Call ApiHealth')");

        // Act
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()).ConfigureAwait(false);

        // Assert
        var responseElement = component.Find("p:contains('API Response:')");
        await Assert.That(responseElement).IsNotNull();
        await Assert.That(responseElement.TextContent).Contains("Hello from handler");
    }
}
