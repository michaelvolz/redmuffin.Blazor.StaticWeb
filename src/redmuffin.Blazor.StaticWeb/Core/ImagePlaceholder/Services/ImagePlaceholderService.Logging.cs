namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Logging partial class for ImagePlaceholderService containing LoggerMessage delegates.
/// </summary>
public sealed partial class ImagePlaceholderService
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Failed to stop shimmer for element {ElementId}: {ErrorMessage}")]
    private static partial void LogShimmerError(ILogger logger, string elementId, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Error handling image load for element {ElementId}, item {ItemLink}: {ErrorMessage}")]
    private static partial void LogImageLoadHandlingError(ILogger logger, string elementId, string itemLink, string errorMessage, Exception exception);
}