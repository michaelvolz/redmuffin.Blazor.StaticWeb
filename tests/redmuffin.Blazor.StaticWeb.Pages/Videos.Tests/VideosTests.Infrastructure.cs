using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Videos;

namespace redmuffin.Blazor.StaticWeb.Pages.Videos.Tests;

[Category("Feature:Videos")]
public sealed partial class VideosTests
{
    [Test]
    public async Task Videos_Should_Check_Fallback_Placeholder_Status()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video", "Test excerpt", "https://example.com/video1") with
            {
                Cover = null,
            },
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        scope.ImagePlaceholderService_Mock.SetupFallbackStatus(testVideos[0].Link, true);
        scope.ImagePlaceholderService_Mock.SetupFallbackReason(
            testVideos[0].Link,
            "Image failed to load"
        );

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert
            .That(component.Find("img").GetAttribute("src"))
            .IsEqualTo(scope.ImagePlaceholderService_Mock.GetDefaultPlaceholder());
        await Assert
            .That(component.Find(".fallback-placeholder-overlay").TextContent)
            .Contains("Image failed to load");
        await Assert
            .That(
                scope.ImagePlaceholderService_Mock.FallbackPlaceholderCalls.Any(call =>
                    call.ItemLink == testVideos[0].Link && call.HasFallback
                )
            )
            .IsTrue();
    }

    [Test]
    [Category("Smoke")]
    public async Task Videos_Should_Display_Videos_When_Available()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video 1", "Test excerpt 1", "https://example.com/video1"),
            CreateTestVideo("2", "Test Video 2", "Test excerpt 2", "https://example.com/video2"),
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert.That(component.FindAll(".video-card")).Count().IsEqualTo(2);
        await Assert.That(component.Markup).Contains("Test Video 1");
        await Assert.That(component.Markup).Contains("Test Video 2");
    }

    [Test]
    public async Task Videos_Should_Populate_Image_Cache_On_Load()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video", "Test excerpt", "https://example.com/video1"),
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Await background refresh so re-render does not invalidate the img event handler
        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        var image = component.Find("img");
        var loadedUrl = image.GetAttribute("src");
        await image.TriggerEventAsync("onload", EventArgs.Empty).ConfigureAwait(false);

        // Assert
        var loadCall = scope.ImagePlaceholderService_Mock.ImageLoadCalls.Single();
        var shimmerInvocations = scope.BUnitContext.JSInterop.Invocations["eval"];
        await Assert.That(loadCall.ItemLink).IsEqualTo(testVideos[0].Link);
        await Assert.That(loadCall.LoadSuccess).IsTrue();
        await Assert.That(loadCall.ImageUrlCache[testVideos[0].Link!]).IsEqualTo(loadedUrl);
        await Assert.That(shimmerInvocations).Count().IsEqualTo(1);
        await Assert.That(shimmerInvocations[0].Arguments[0]?.ToString()).Contains("shimmer-1");
    }

    [Test]
    public async Task Videos_Should_Use_Image_Placeholder_Service()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testVideos = new List<RaindropItem>
        {
            CreateTestVideo("1", "Test Video", "Test excerpt", "https://example.com/video1"),
        };

        scope.Mediator_Mock.SetupLoad(testVideos);
        scope.Mediator_Mock.SetupRefresh(testVideos);

        scope.ImagePlaceholderService_Mock.SetupImageUrl(
            testVideos[0].Link,
            "data:image/svg+xml;base64,test"
        );

        // Act
        var component = scope.BUnitContext.Render<Videos>();

        // Assert
        await Assert
            .That(component.Find("img").GetAttribute("src"))
            .IsEqualTo("data:image/svg+xml;base64,test");
    }
}
