using Bunit;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Pages.Videos;
using redmuffin.Blazor.StaticWeb.Tests.Support.Images;
using redmuffin.Blazor.StaticWeb.Tests.Support.Logging;
using redmuffin.Blazor.StaticWeb.Tests.Support.Mediator;

namespace redmuffin.Blazor.StaticWeb.Pages.Videos.Tests;

[Category("Feature:Videos")]
public sealed partial class VideosTests
{
    /// <summary>
    ///     Creates a test scope with all necessary dependencies for Videos component testing.
    /// </summary>
    /// <returns>A configured TestScope instance</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    /// <summary>
    ///     Creates a test video item for testing purposes.
    /// </summary>
    /// <param name="id">The video ID</param>
    /// <param name="title">The video title</param>
    /// <param name="excerpt">The video excerpt</param>
    /// <param name="link">The video link</param>
    /// <returns>A configured RaindropItem for testing</returns>
    private static RaindropItem CreateTestVideo(
        string id,
        string title,
        string excerpt,
        string link
    )
    {
        return new RaindropItem
        {
            Id = int.Parse(id),
            Title = title,
            Excerpt = excerpt,
            Link = link,
            Cover = $"https://example.com/cover{id}.jpg",
            Created = DateTime.UtcNow,
            Type = "video",
            Domain = "example.com",
        };
    }

    /// <summary>
    ///     Modern test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestScope(string baseUri = "http://localhost:5000/")
        {
            BUnitContext = new BunitContext();
            NavigationManager = new NavigationManager_Mock(baseUri);
            Logger = new Logger_Spy<Videos>();
            Mediator_Mock = new RaindropMediator_Mock();
            ImagePlaceholderService_Mock = new ImagePlaceholderService_Mock();
            ImageUrlResolver = new ImageUrlResolver_Mock();
        }

        public BunitContext BUnitContext { get; }
        public NavigationManager_Mock NavigationManager { get; }
        public Logger_Spy<Videos> Logger { get; }
        public RaindropMediator_Mock Mediator_Mock { get; }
        public ImagePlaceholderService_Mock ImagePlaceholderService_Mock { get; }
        public ImageUrlResolver_Mock ImageUrlResolver { get; }

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<Videos>>(Logger);
            BUnitContext.Services.AddSingleton<IMediator>(Mediator_Mock);
            BUnitContext.Services.AddSingleton<IImagePlaceholderService>(
                ImagePlaceholderService_Mock
            );
            BUnitContext.Services.AddSingleton<IImageUrlResolver>(ImageUrlResolver);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;

            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    /// <summary>
    ///     Mock NavigationManager for testing.
    /// </summary>
    public sealed class NavigationManager_Mock : NavigationManager
    {
        public NavigationManager_Mock(string baseUri = "http://localhost:5000/")
        {
            Initialize(baseUri, baseUri);
        }

        public string? NavigatedTo { get; private set; }
        public bool NavigationCalled { get; private set; }

        public void Reset()
        {
            NavigatedTo = null;
            NavigationCalled = false;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
            NavigationCalled = true;
        }
    }
}
