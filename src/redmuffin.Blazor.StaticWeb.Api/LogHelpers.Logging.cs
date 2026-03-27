using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api;

public static partial class LogHelpers
{
    [LoggerMessage(1, LogLevel.Information, "This is a test log message.", EventName = nameof(LogTestMessage))]
    public static partial void LogTestMessage(ILogger logger);
}
