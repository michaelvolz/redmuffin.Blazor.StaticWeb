using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api;

/// <summary>
///     LoggerMessage delegates for LogHelpers class.
/// </summary>
public static partial class LogHelpers
{
    private static readonly Action<ILogger, Exception?> LogTestMessageInternal =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "LogTestMessage"),
            "This is a test log message.");
}