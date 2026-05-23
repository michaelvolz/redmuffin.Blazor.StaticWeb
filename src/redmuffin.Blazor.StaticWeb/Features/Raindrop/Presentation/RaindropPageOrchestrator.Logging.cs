using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;

public static partial class RaindropPageOrchestrator
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Failed to populate image cache, images may load slower")]
    private static partial void LogImageCacheWarning(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Error loading cached items, falling back to fresh data")]
    private static partial void LogCacheLoadFallback(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Error,
        Message = "Failed to fetch fresh items after cache failure")]
    private static partial void LogFreshFetchFailure(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Failed to cache refreshed data, data will still be displayed")]
    private static partial void LogCacheRefreshWarning(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Error,
        Message = "Network error during manual refresh")]
    private static partial void LogNetworkRefreshError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Error,
        Message = "Request timeout during manual refresh")]
    private static partial void LogRefreshTimeout(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Error,
        Message = "Unexpected error during manual refresh")]
    private static partial void LogUnexpectedRefreshError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Error,
        Message = "Failed to fetch and cache items")]
    private static partial void LogFetchAndCacheFailure(ILogger logger, Exception exception);
}
