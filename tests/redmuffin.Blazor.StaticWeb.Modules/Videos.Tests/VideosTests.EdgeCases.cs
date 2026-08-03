using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Videos;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Components;

namespace redmuffin.Blazor.StaticWeb.Modules.Videos.Tests;

[Category("Feature:Videos")]
public sealed partial class VideosTests
{
    [Test]
    [Category("Smoke")]
    public async Task Videos_Should_Display_Error_Message_When_API_Fails()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.Mediator_Mock.SetupLoadFailure("API request failed");
        scope.Mediator_Mock.SetupRefreshFailure("API request failed");

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.Find(".callout.alert")).IsNotNull();
        await Assert.That(component.Markup).Contains("Unable to load items");
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

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.Markup).Contains("No Excerpt Available");
    }

    [Test]
    [Category("Smoke")]
    public async Task Videos_Should_Handle_Videos_With_Missing_Titles()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "", "Test excerpt", "https://example.com/video1"),
            CreateTestVideo("2", null!, "Test excerpt", "https://example.com/video2")
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.Markup).Contains("No Title Available");
    }

    [Test]
    [Category("Smoke")]
    public async Task Videos_Should_Render_Successfully_With_No_Videos()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.Mediator_Mock.SetupLoad([]);
        scope.Mediator_Mock.SetupRefresh([]);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert — empty state container is visible
        await Assert.That(
            component.Find($"#{RaindropItemList.EmptyStateElementId}")).IsNotNull();
    }
}
