using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api;

/// <summary>
/// LoggerMessage helpers for better performance
/// </summary>
public static class LogHelpers
{
    private static readonly Action<ILogger, Exception?> LogTestMessageInternal =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "LogTestMessage"),
            "This is a test log message.");

    public static void LogTestMessage(ILogger logger) => LogTestMessageInternal(logger, null);
}
