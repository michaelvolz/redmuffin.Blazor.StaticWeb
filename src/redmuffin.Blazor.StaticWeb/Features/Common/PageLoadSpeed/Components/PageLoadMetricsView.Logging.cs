namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class PageLoadMetricsView
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Page load metrics initialization failed")]
    private static partial void LogInitializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Page load metrics fallback timing failed")]
    private static partial void LogFallbackMetricsFailed(ILogger logger, Exception exception);
}
