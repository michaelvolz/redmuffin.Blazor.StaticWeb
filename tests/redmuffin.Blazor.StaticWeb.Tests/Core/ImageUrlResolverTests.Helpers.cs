using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
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
            Type = "link"
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
            ImageValidationService_Mock = new Mock<IImageValidator>();
            ImagePlaceholderService_Mock = new Mock<IImagePlaceholderService>();
            Logger = new Logger_Spy<ImageUrlResolver>();

            Service = new ImageUrlResolver(
                ImageValidationService_Mock.Object,
                ImagePlaceholderService_Mock.Object,
                Logger);
        }

        /// <summary>
        ///     Gets the mock for IImageValidator.
        /// </summary>
        internal Mock<IImageValidator> ImageValidationService_Mock { get; }

        /// <summary>
        ///     Gets the mock for IImagePlaceholderService.
        /// </summary>
        internal Mock<IImagePlaceholderService> ImagePlaceholderService_Mock { get; }

        /// <summary>
        ///     Gets the test logger for ImageUrlResolver.
        /// </summary>
        internal Logger_Spy<ImageUrlResolver> Logger { get; }

        /// <summary>
        ///     Gets the ImageUrlResolver instance under test.
        /// </summary>
        internal ImageUrlResolver Service { get; }

        /// <summary>
        ///     Disposes the test scope and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    /// <summary>
    ///     Test logger implementation for capturing log messages during tests.
    /// </summary>
    /// <typeparam name="T">The category type for the logger.</typeparam>
    public sealed class Logger_Spy<T> : ILogger<T>
    {
        private readonly List<LogEntry> _logs = [];

        /// <summary>
        ///     Gets the captured log entries.
        /// </summary>
        public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

        /// <summary>
        ///     Clears all captured log entries.
        /// </summary>
        public void Clear()
        {
            _logs.Clear();
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _logs.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
    }

    /// <summary>
    ///     Represents a captured log entry for testing.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>
        ///     Gets or sets the log level.
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        ///     Gets or sets the event ID.
        /// </summary>
        public EventId EventId { get; set; }

        /// <summary>
        ///     Gets or sets the log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the exception, if any.
        /// </summary>
        public Exception? Exception { get; set; }
    }
}
