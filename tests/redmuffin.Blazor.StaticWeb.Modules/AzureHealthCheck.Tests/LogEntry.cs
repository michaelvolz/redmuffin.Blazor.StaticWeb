using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Tests;

public sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
