using Bunit;
using redmuffin.Blazor.StaticWeb.Features.ApiExamplePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ApiExamplePage;

[Category("Feature:ApiExample")]
[Category("Unit")]
public partial class CallApiExampleTests
{
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
            var heading = component.Find("h1");
            await Assert.That(heading).IsNotNull();
            await Assert.That(heading.TextContent).Contains("Call API Example");

            // Button has proper Foundation class
            var button = component.Find("button.button");
            await Assert.That(button).IsNotNull();
        }
    }

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
}