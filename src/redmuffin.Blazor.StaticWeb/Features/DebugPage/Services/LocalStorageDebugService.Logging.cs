namespace redmuffin.Blazor.StaticWeb.Features.DebugPage.Services;

public partial class LocalStorageDebugService
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "LocalStorage diagnostics completed: Available={IsAvailable}, BlazoredWorking={BlazoredWorking}, UsedStorage={UsedBytes}MB")]
    private static partial void LogDiagnosticsCompleted(ILogger logger, bool isAvailable, bool blazoredWorking, string usedBytes);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to run localStorage diagnostics")]
    private static partial void LogDiagnosticsFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "localStorage availability test failed")]
    private static partial void LogLocalStorageTestFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Blazored localStorage test failed: expected '{Expected}', got '{Actual}'")]
    private static partial void LogBlazoredTestFailed(ILogger logger, string expected, string? actual);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Blazored localStorage service test failed")]
    private static partial void LogBlazoredServiceFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Failed to get storage info")]
    private static partial void LogStorageInfoFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "JSON serialization test failed")]
    private static partial void LogJsonTestFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Failed to get existing cache keys")]
    private static partial void LogCacheKeysFailed(ILogger logger, Exception exception);
}
