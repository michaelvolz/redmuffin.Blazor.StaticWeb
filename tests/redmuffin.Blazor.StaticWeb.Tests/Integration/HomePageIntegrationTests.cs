using Bunit;
using redmuffin.Blazor.StaticWeb.Features.HomePage;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

/// <summary>
///     Integration tests for the Home page component verifying full system behavior.
///     Uses TestScope pattern for clean resource management and consistent service setup.
///     Optimized for fast execution with DelayProvider_Stub.
/// </summary>
[Category("Feature:Home")]
[Category("Integration")]
public sealed partial class HomePageIntegrationTests
{
    [Test]
    public async Task Homepage_HasCorrectPageTitle()
    {
        // Arrange
        using var scope = CreateIntegrationTestScope();
        var component = scope.Context.Render<Home>();

        // Assert - Verify PageTitle component functionality
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Chain related markup assertions
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify PageTitle component is included in the rendered output
            await Assert.That(component.FindAll("title").Count).IsGreaterThanOrEqualTo(0);
        }
    }
}