using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.VideosPage;

public sealed partial class VideosTests
{
    [Test]
    public async Task Videos_Should_Display_Error_Message_When_API_Fails()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.RaindropAPI_Mock.SetupVideosException(new HttpRequestException("API request failed"));

        // Act
        var component = scope.BUnitContext.Render<Videos>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to update

        // Assert
        await Assert.That(component.Find(".callout.alert")).IsNotNull();
        await Assert.That(component.Markup).Contains("Exception fetching videos");
    }

    [Test]
    public async Task Videos_Should_Handle_Videos_With_Missing_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video", "", "https://example.com/video1"),
            CreateTestVideo("2", "Test Video 2", null!, "https://example.com/video2")
        };

        scope.RaindropAPI_Mock.SetupVideos(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to update

        // Assert
        await Assert.That(component.Markup).Contains("No Excerpt Available");
    }

    [Test]
    public async Task Videos_Should_Handle_Videos_With_Missing_Titles()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "", "Test excerpt", "https://example.com/video1"),
            CreateTestVideo("2", null!, "Test excerpt", "https://example.com/video2")
        };

        scope.RaindropAPI_Mock.SetupVideos(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();
        await Task.Delay(100).ConfigureAwait(false); // Allow component to update

        // Assert
        await Assert.That(component.Markup).Contains("No Title Available");
    }

    [Test]
    public async Task Videos_Should_Render_Successfully_With_No_Videos()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.RaindropAPI_Mock.SetupVideos(new List<RaindropItem>());

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.Find("h1").TextContent).Contains("Programming & AI Video Hub");
        await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(0);
    }
}