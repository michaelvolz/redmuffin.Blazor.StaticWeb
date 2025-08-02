using System.Text;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Helper classes and methods for PlaceholderGenerationServiceTests.
/// </summary>
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
    public sealed class TestScope : IDisposable
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
        public Logger_Spy<PlaceholderGenerationService> Logger { get; }

        /// <summary>
        ///     Gets the PlaceholderGenerationService instance under test.
        /// </summary>
        public PlaceholderGenerationService Service { get; }

        /// <summary>
        ///     Decodes a base64-encoded SVG data URI to its original SVG string.
        /// </summary>
        /// <param name="dataUri">The base64-encoded SVG data URI</param>
        /// <returns>The decoded SVG string</returns>
        public static string DecodeSvgFromDataUri(string dataUri)
        {
            const string prefix = "data:image/svg+xml;base64,";
            if (!dataUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid data URI format. Expected to start with '{prefix}'", nameof(dataUri));

            var base64Data = dataUri[prefix.Length..];
            var svgBytes = Convert.FromBase64String(base64Data);
            return Encoding.UTF8.GetString(svgBytes);
        }

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