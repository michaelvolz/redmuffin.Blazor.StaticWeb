using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Helper classes and methods for ImageValidationCacheServiceTests.
/// </summary>
public sealed partial class ImageValidationCacheServiceTests
{
    /// <summary>
    ///     Creates a test scope for ImageValidationCacheService tests.
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
    ///     Test scope for ImageValidationCacheService tests with dependency injection setup.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        private bool _disposed;

        public TestScope()
        {
            SimpleImageValidationServiceMock = new Mock<ISimpleImageValidationService>();
            ImagePlaceholderServiceMock = new Mock<IImagePlaceholderService>();
            Logger = new TestLogger<ImageValidationCacheService>();

            Service = new ImageValidationCacheService(
                SimpleImageValidationServiceMock.Object,
                ImagePlaceholderServiceMock.Object,
                Logger);
        }

        /// <summary>
        ///     Gets the mock for ISimpleImageValidationService.
        /// </summary>
        public Mock<ISimpleImageValidationService> SimpleImageValidationServiceMock { get; }

        /// <summary>
        ///     Gets the mock for IImagePlaceholderService.
        /// </summary>
        public Mock<IImagePlaceholderService> ImagePlaceholderServiceMock { get; }

        /// <summary>
        ///     Gets the test logger for ImageValidationCacheService.
        /// </summary>
        public TestLogger<ImageValidationCacheService> Logger { get; }

        /// <summary>
        ///     Gets the ImageValidationCacheService instance under test.
        /// </summary>
        public ImageValidationCacheService Service { get; }

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
    public sealed class TestLogger<T> : ILogger<T>
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