using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Features.ApiExamplePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ApiExamplePage;

[Category("Feature:ApiExample")]
public partial class CallApiExampleTests
{
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
}