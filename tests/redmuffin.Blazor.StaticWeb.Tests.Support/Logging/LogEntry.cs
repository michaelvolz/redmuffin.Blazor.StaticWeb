using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Tests.Support.Logging;

/// <summary>
///     Captured log entry produced by <see cref="Logger_Spy{T}" />.
/// </summary>
public sealed record LogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception
);
