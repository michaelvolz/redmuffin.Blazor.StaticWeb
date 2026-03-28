using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ApiExamplePage;

[Category("Feature:ApiExample")]
public sealed partial class CallApiExampleTests
{
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
}