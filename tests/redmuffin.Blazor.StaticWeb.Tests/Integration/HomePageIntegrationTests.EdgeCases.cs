using Bunit;
using redmuffin.Blazor.StaticWeb.Features.Pages.HomePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

public sealed partial class HomePageIntegrationTests
{
    [Test]
    public async Task Homepage_RendersSuccessfully_WithoutErrors()
    {
        // Arrange
        using var scope = CreateIntegrationTestScope();
        var component = scope.Context.Render<Home>();

        // Assert - Verify successful rendering and structure
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            await Assert.That(component.FindAll("div[style='font-size:2rem;']").Count).IsEqualTo(1);
        }
    }
}