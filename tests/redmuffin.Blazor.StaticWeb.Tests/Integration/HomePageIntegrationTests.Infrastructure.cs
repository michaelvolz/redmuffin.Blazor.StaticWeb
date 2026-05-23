using Bunit;
using redmuffin.Blazor.StaticWeb.Features.HomePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

[Category("Feature:Home")]
public sealed partial class HomePageIntegrationTests
{
    [Test]
    public async Task Homepage_DisplaysHeadingAndEmojis()
    {
        // Arrange
        using var scope = CreatePortSpecificTestScope("http://localhost:4280/");
        var component = scope.Context.Render<Home>();

        // Act & Assert - Use chaining for related markup assertions
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            await Assert.That(component.Find("div[style='font-size:2rem;']").TextContent)
                .Contains("😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇");
        }
    }
}