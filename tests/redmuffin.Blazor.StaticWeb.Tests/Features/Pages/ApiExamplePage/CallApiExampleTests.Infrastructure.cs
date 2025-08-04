using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ApiExamplePage;

public sealed partial class CallApiExampleTests
{
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
}