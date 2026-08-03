using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug.CacheReset;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class CacheReset
#pragma warning restore MA0049
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
