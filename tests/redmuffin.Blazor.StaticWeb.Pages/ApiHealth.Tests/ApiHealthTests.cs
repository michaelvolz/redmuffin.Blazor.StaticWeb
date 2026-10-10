using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ApiHealthPage = redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth;

namespace redmuffin.Blazor.StaticWeb.Pages.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
[Category("Unit")]
public sealed partial class ApiHealthTests
{
    [Test]
    public async Task Displays_api_response_when_button_clicked()
    {
        // Arrange
        using var scope = CreateTestScope("Hello from handler");
        var component = scope.BUnitContext.Render<ApiHealthPage>();
        var button = component.Find("button.button");

        // Act
        await button
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs())
            .ConfigureAwait(false);

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
        var component =
            scope.BUnitContext.Render<global::redmuffin.Blazor.StaticWeb.Pages.ApiHealth.ApiHealth>();

        // Assert - idle state before any click
        var emptyState = component.Find("div.empty-state");
        await Assert.That(emptyState.TextContent).Contains("No checks have been run yet");
        await Assert.That(component.FindAll("blockquote")).IsEmpty();

        // Act - the health-check button is enabled and clickable
        var button = component.Find("button.button");
        await Assert.That(button.HasAttribute("disabled")).IsFalse();
        await button
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs())
            .ConfigureAwait(false);

        // Assert - the click was handled and the empty state is gone
        await Assert.That(component.FindAll("div.empty-state")).IsEmpty();
    }
}
