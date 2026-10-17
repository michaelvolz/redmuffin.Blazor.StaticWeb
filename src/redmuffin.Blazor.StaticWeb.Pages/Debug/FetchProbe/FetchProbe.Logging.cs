using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug.FetchProbe;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class FetchProbe
#pragma warning restore MA0049
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting cover fetch probe"
    )]
    private static partial void LogProbeStarted(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Cover fetch probe completed"
    )]
    private static partial void LogProbeCompleted(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Cover fetch probe failed")]
    private static partial void LogProbeFailed(ILogger logger, Exception exception);
}
