namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Failed to stop shimmer for element {ElementId}: {ErrorMessage}")]
    private static partial void LogShimmerError(ILogger logger, string elementId, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Background validation failed for video {VideoLink}: {ErrorMessage}")]
    private static partial void LogBackgroundValidationFailure(ILogger logger, string videoLink, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Debug,
        Message = "Starting background validation task for {VideoCount} videos")]
    private static partial void LogBackgroundTaskStart(ILogger logger, int videoCount);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "Background validation completed for {VideoCount} videos in {ElapsedMs}ms")]
    private static partial void LogBackgroundValidationComplete(ILogger logger, int videoCount, long elapsedMs);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Error handling image load for element {ElementId}, video {VideoLink}: {ErrorMessage}")]
    private static partial void LogImageLoadHandlingError(ILogger logger, string elementId, string videoLink, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "Failed to populate image cache for cached videos, images may load slower")]
    private static partial void LogImageCacheWarningCached(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Warning,
        Message = "Error loading cached videos, falling back to fresh data")]
    private static partial void LogCacheLoadError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Warning,
        Message = "Failed to cache fresh video data, continuing without caching")]
    private static partial void LogCacheStoreError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Warning,
        Message = "Failed to populate image cache, continuing with basic display")]
    private static partial void LogImageCacheError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "Network error during background refresh, will retry later")]
    private static partial void LogNetworkErrorBackground(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Warning,
        Message = "Request timeout during background refresh, will retry later")]
    private static partial void LogTimeoutBackground(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Warning,
        Message = "Failed to cache refreshed video data, data will still be displayed")]
    private static partial void LogCacheRefreshError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Warning,
        Message = "Failed to populate image cache during refresh, images may load slower")]
    private static partial void LogImageCacheRefreshError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Information,
        Message = "Loaded {Count} videos from cache")]
    private static partial void LogVideosLoadedFromCache(ILogger logger, int count);

    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Information,
        Message = "No cached video data available, fetching fresh data")]
    private static partial void LogNoCachedData(ILogger logger);

    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Error,
        Message = "Failed to fetch fresh videos after cache failure")]
    private static partial void LogFetchError(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1017,
        Level = LogLevel.Error,
        Message = "Unexpected error refreshing videos in background")]
    private static partial void LogUnexpectedErrorBackground(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1018,
        Level = LogLevel.Error,
        Message = "Network error during manual refresh")]
    private static partial void LogNetworkErrorManual(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1019,
        Level = LogLevel.Error,
        Message = "Request timeout during manual refresh")]
    private static partial void LogTimeoutManual(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Error,
        Message = "Unexpected error during manual refresh")]
    private static partial void LogUnexpectedErrorManual(ILogger logger, Exception exception);
}