using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

public sealed class Logger_Spy<T> : ILogger<T>
{
    private readonly List<LogEntry> _logEntries = [];

    public IReadOnlyList<LogEntry> LogEntries => _logEntries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logEntries.Add(
            new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }
}
