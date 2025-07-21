using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api;

/// <summary>
///     LoggerMessage helpers for better performance using partial class pattern.
/// </summary>
public static partial class LogHelpers
{
    /// <summary>
    ///     Logs a test message for validation purposes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public static void LogTestMessage(ILogger logger)
    {
        LogTestMessageInternal(logger, null);
    }
}