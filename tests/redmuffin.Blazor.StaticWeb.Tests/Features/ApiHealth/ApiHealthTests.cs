using Bunit;
using Microsoft.Extensions.DependencyInjection;

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
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth>();

        // Assert
        using (Assert.Multiple())
        {
            var heading = component.Find("h1");
            await Assert.That(heading).IsNotNull();
            await Assert.That(heading.TextContent).Contains("API Health Check");

            var button = component.Find("button.button");
            await Assert.That(button).IsNotNull();
            await Assert.That(button.TextContent).Contains("Run Health Check");
        }
    }

    [Test]
    public async Task Displays_api_response_when_button_clicked()
    {
        // Arrange
        using var scope = CreateTestScope("Hello from handler");
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth>();
        var button = component.Find("button.button");

        // Act
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()).ConfigureAwait(false);

        // Assert
        var responseBlock = component.Find("blockquote");
        await Assert.That(responseBlock).IsNotNull();
        await Assert.That(responseBlock.TextContent).Contains("Hello from handler");

        var checkRows = component.FindAll("div.check-row");
        await Assert.That(checkRows).Count().IsEqualTo(2);
        await Assert.That(checkRows[0].TextContent).Contains("Message Valid");
        await Assert.That(checkRows[1].TextContent).Contains("Latency");
    }

    [Test]
    public async Task Displays_empty_state_on_initial_load()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth>();

        // Assert
        var emptyState = component.Find("div.empty-state");
        await Assert.That(emptyState).IsNotNull();
        await Assert.That(emptyState.TextContent).Contains("No checks have been run yet");
    }
}
