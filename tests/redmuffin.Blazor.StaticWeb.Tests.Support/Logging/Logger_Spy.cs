using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Tests.Support.Logging;

/// <summary>
///     Test logger that captures every log entry for assertions.
/// </summary>
public sealed class Logger_Spy<T> : ILogger<T>
{
    private readonly List<LogEntry> _logEntries = [];

    /// <summary>
    ///     Gets the captured log entries in call order.
    /// </summary>
    public IReadOnlyList<LogEntry> LogEntries => _logEntries;

    /// <summary>
    ///     Clears the captured log entries.
    /// </summary>
    public void Reset()
    {
        _logEntries.Clear();
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        _logEntries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }
}
