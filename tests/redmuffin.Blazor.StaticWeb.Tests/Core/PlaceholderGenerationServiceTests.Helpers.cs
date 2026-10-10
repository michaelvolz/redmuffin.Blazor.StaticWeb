using System.Text;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Tests.Support.Logging;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Helper classes and methods for PlaceholderGenerationServiceTests.
/// </summary>
[Category("Feature:Core")]
public sealed partial class PlaceholderGenerationServiceTests
{
    /// <summary>
    ///     Creates a test scope for PlaceholderGenerationService tests.
    /// </summary>
    /// <returns>A configured test scope.</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    /// <summary>
    ///     Test scope for PlaceholderGenerationService tests with dependency injection setup.
    /// </summary>
    internal sealed class TestScope : IDisposable
    {
        private bool _disposed;

        public TestScope()
        {
            Logger = new Logger_Spy<PlaceholderGenerationService>();
            Service = new PlaceholderGenerationService(Logger);
        }

        /// <summary>
        ///     Gets the test logger for PlaceholderGenerationService.
        /// </summary>
        internal Logger_Spy<PlaceholderGenerationService> Logger { get; }

        /// <summary>
        ///     Gets the PlaceholderGenerationService instance under test.
        /// </summary>
        internal PlaceholderGenerationService Service { get; }

        /// <summary>
        ///     Decodes a base64-encoded SVG data URI to its original SVG string.
        /// </summary>
        /// <param name="dataUri">The base64-encoded SVG data URI</param>
        /// <returns>The decoded SVG string</returns>
        public static string DecodeSvgFromDataUri(string dataUri)
        {
            const string prefix = "data:image/svg+xml;base64,";
            if (!dataUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Invalid data URI format. Expected to start with '{prefix}'",
                    nameof(dataUri)
                );

            var base64Data = dataUri[prefix.Length..];
            var svgBytes = Convert.FromBase64String(base64Data);
            return Encoding.UTF8.GetString(svgBytes);
        }

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
