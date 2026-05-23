namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage.CacheResetPage;

public partial class CacheReset
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting cache reset operation")]
    private static partial void LogCacheResetStarted(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Cache reset completed successfully. Items cleared: {ItemsCleared}")]
    private static partial void LogCacheResetCompleted(ILogger logger, int itemsCleared);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Error occurred during cache reset")]
    private static partial void LogCacheResetError(ILogger logger, Exception exception);
}
