using Bunit;
using redmuffin.Blazor.StaticWeb.Pages.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

[Category("Feature:Home")]
[Category("Integration")]
public sealed partial class HomePageIntegrationTests
{
    [Test]
    public async Task Homepage_RendersSuccessfully_WithHeadingAndEmojiRegion()
    {
        // Arrange
        using var scope = CreateIntegrationTestScope();
        var component = scope.Context.Render<Home>();

        // Assert - the page renders with its heading and a labelled emoji region
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            await Assert
                .That(component.Find("div[role='img']").GetAttribute("aria-label"))
                .IsNotNull()
                .And.IsNotEmpty();
        }
    }
}
