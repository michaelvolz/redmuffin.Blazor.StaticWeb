namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class AppStartMetricsView
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "App start metrics initialization failed")]
    private static partial void LogInitializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "App start metrics fallback timing failed after primary metrics error: {OriginalMessage}")]
    private static partial void LogFallbackMetricsFailed(ILogger logger, string originalMessage, Exception fallbackException);
}
