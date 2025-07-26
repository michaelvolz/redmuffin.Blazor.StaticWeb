using Microsoft.Extensions.Logging;

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
}