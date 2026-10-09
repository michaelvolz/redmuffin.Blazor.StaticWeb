using LightMock.Generator;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Helper classes and methods for ImageUrlResolverTests.
/// </summary>
[Category("Feature:Core")]
public sealed partial class ImageUrlResolverTests
{
    /// <summary>
    ///     Creates a test scope for ImageUrlResolver tests.
    /// </summary>
    /// <returns>A configured test scope.</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    /// <summary>
    ///     Creates a test RaindropItem with the specified link and cover.
    /// </summary>
    /// <param name="link">The item link.</param>
    /// <param name="cover">The cover image URL.</param>
    /// <returns>A configured RaindropItem for testing.</returns>
    private static RaindropItem CreateTestItem(string link, string cover)
    {
        return new RaindropItem
        {
            Link = link,
            Cover = cover,
            Title = "Test Item",
            Excerpt = "Test excerpt",
            Domain = "example.com",
            Created = DateTime.UtcNow,
            Type = "link",
        };
    }

    /// <summary>
    ///     Test scope for ImageUrlResolver tests with dependency injection setup.
    /// </summary>
    internal sealed class TestScope : IDisposable
    {
        private bool _disposed;

        public TestScope()
        {
            ImagePlaceholderService_Mock = new Mock<IImagePlaceholderService>();

            Service = new ImageUrlResolver(ImagePlaceholderService_Mock.Object);
        }

        /// <summary>
        ///     Gets the mock for IImagePlaceholderService.
        /// </summary>
        internal Mock<IImagePlaceholderService> ImagePlaceholderService_Mock { get; }

        /// <summary>
        ///     Gets the ImageUrlResolver instance under test.
        /// </summary>
        internal ImageUrlResolver Service { get; }

        /// <summary>
        ///     Disposes the test scope and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
        }
    }
}
