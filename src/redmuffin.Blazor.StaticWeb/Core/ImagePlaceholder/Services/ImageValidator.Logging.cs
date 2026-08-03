namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

internal sealed partial class ImageValidator
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Starting image validation for URL: {ImageUrl}")]
    private static partial void LogImageValidationStarted(ILogger logger, string imageUrl);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Image validation successful for URL: {ImageUrl}")]
    private static partial void LogImageValidationSuccess(ILogger logger, string imageUrl);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Image validation failed for URL: {ImageUrl}, Reason: {Reason}")]
    private static partial void LogImageValidationFailed(ILogger logger, string imageUrl, string reason);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Image validation failed for URL: {ImageUrl}, Reason: {Reason}")]
    private static partial void LogImageValidationFailed(ILogger logger, string imageUrl, string reason, Exception exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Cache hit for image URL: {ImageUrl}")]
    private static partial void LogCacheHit(ILogger logger, string imageUrl);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Cache miss for image URL: {ImageUrl}")]
    private static partial void LogCacheMiss(ILogger logger, string imageUrl);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Cache miss for image URL: {ImageUrl}")]
    private static partial void LogCacheMiss(ILogger logger, string imageUrl, Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Failed to perform cache cleanup")]
    private static partial void LogCacheCleanupFailed(ILogger logger, Exception exception);
}
