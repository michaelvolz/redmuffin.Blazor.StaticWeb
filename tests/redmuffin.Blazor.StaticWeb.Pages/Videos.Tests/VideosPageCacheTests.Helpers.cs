using Bunit;
using Mediator;
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

/// <summary>
///     Helper methods and infrastructure for VideosPageCacheTests.
/// </summary>
[Category("Feature:Videos")]
public partial class VideosPageCacheTests
{
    /// <summary>
    ///     Creates a new test scope with standard configuration.
    /// </summary>
    /// <returns>A configured test scope ready for component testing.</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    /// <summary>
    ///     Creates a test RaindropItem for testing purposes.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <param name="title">The item title.</param>
    /// <param name="excerpt">The item excerpt.</param>
    /// <param name="link">The item link (optional).</param>
    /// <returns>A configured test RaindropItem.</returns>
    private static RaindropItem CreateTestVideo(
        string id,
        string title,
        string excerpt,
        string? link = null
    )
    {
        return new RaindropItem
        {
            Id = long.Parse(id),
            Title = title,
            Excerpt = excerpt,
            Link = link ?? $"https://example.com/video/{id}",
            Cover = $"https://example.com/cover/{id}.jpg",
            Created = DateTime.UtcNow.AddDays(-1),
        };
    }

    /// <summary>
    ///     Test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        /// <summary>
        ///     Gets the bUnit test context for component rendering.
        /// </summary>
        public BunitContext Context { get; } = new();

        /// <summary>
        ///     Gets the Mediator mock used by the Videos page after Step 3 cutover.
        /// </summary>
        public RaindropMediator_Mock Mediator_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock for IImagePlaceholderService.
        /// </summary>
        public ImagePlaceholderService_Mock ImagePlaceholderService_Mock { get; } = new();

        /// <summary>
        ///     Gets the mock for IImageUrlResolver.
        /// </summary>
        public ImageUrlResolver_Mock ImageUrlResolver_Mock { get; } = new();

        /// <summary>
        ///     Gets the spy logger for Videos component.
        /// </summary>
        public Logger_Spy<Videos> Logger { get; } = new();

        /// <summary>
        ///     Configures the test scope with standard services for component testing.
        /// </summary>
        /// <returns>The configured test scope for method chaining.</returns>
        public TestScope WithStandardServices()
        {
            Context.Services.AddSingleton<IMediator>(Mediator_Mock);
            Context.Services.AddSingleton<IImagePlaceholderService>(ImagePlaceholderService_Mock);
            Context.Services.AddSingleton<IImageUrlResolver>(ImageUrlResolver_Mock);
            Context.Services.AddSingleton<ILogger<Videos>>(Logger);
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            return this;
        }

        /// <summary>
        ///     Disposes of test resources.
        /// </summary>
        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
