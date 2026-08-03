using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Videos;

namespace redmuffin.Blazor.StaticWeb.Pages.Videos.Tests;

/// <summary>
///     TUnit tests for Videos component.
/// </summary>
[Category("Feature:Videos")]
[Category("Unit")]
public sealed partial class VideosTests
{
    [Test]
    [Category("Smoke")]
    public async Task Videos_Should_Truncate_Long_Excerpts()
    {
        // Arrange
        using var scope = CreateTestScope();
        var longExcerpt = new string('A', 300); // 300 characters
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video", longExcerpt, "https://example.com/video1")
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.Markup).Contains("...");
        await Assert.That(component.Markup).DoesNotContain(longExcerpt);
    }
}
