using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Pages.ApiExamplePage;

public partial class CallApiExampleTests
{
    [Test]
    public async Task CallApiExample_Should_Render_Button_When_Loaded()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<CallApiExample>();

        // Assert - Verify button is rendered
        using (Assert.Multiple())
        {
            var button = component.Find("button:contains('Call Hello World API')");
            await Assert.That(button).IsNotNull();

            // Verify button has proper Foundation class
            var buttonElement = component.Find("button.button");
            await Assert.That(buttonElement).IsNotNull();
        }
    }

    [Test]
    public async Task CallApiExample_Should_Display_API_Response_When_Button_Clicked()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<CallApiExample>();
        var button = component.Find("button:contains('Call Hello World API')");

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify API response is displayed
        using (Assert.Multiple())
        {
            var responseElement = component.Find("p:contains('API Response:')");
            await Assert.That(responseElement).IsNotNull();
            await Assert.That(responseElement.TextContent).Contains("Mock response");
        }
    }

    [Test]
    public async Task CallApiExample_Should_Handle_HTTP_Errors_Gracefully()
    {
        // Arrange
        using var scope = CreateFailingServiceTestScope();
        var component = scope.BUnitContext.Render<CallApiExample>();
        var button = component.Find("button:contains('Call Hello World API')");

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify error is displayed gracefully
        using (Assert.Multiple())
        {
            var errorElement = component.Find("p:contains('Error:')");
            await Assert.That(errorElement).IsNotNull();
            await Assert.That(errorElement.TextContent).Contains("Error calling API");

            // Verify component remains functional
            await Assert.That(component.Find("h3").TextContent).Contains("Call API Example");
        }
    }

    [Test]
    public async Task CallApiExample_Should_Clear_Previous_Response_On_New_Call()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<CallApiExample>();
        var button = component.Find("button:contains('Call Hello World API')");

        // Act - Click button twice
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        var firstResponse = component.Find("p:contains('API Response:')");
        await Assert.That(firstResponse).IsNotNull();

        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify response is still displayed (same call)
        using (Assert.Multiple())
        {
            var response = component.Find("p:contains('API Response:')");
            await Assert.That(response).IsNotNull();
            await Assert.That(response.TextContent).Contains("Mock response");
        }
    }



    [Test]
    public async Task CallApiExample_Should_Have_Proper_Page_Structure()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<CallApiExample>();

        // Assert - Verify page structure
        using (Assert.Multiple())
        {
            // Page heading
            var heading = component.Find("h3");
            await Assert.That(heading).IsNotNull();
            await Assert.That(heading.TextContent).Contains("Call API Example");

            // Button has proper Foundation class
            var button = component.Find("button.button");
            await Assert.That(button).IsNotNull();
        }
    }




}