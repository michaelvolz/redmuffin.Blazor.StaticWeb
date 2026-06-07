using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

public sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
