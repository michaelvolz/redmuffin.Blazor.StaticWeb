namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

public sealed partial class ImageUrlResolver
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Background validation failed for item: {ItemLink}")]
    private static partial void LogBackgroundValidationFailed(ILogger logger, string itemLink, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Started {TaskCount} background validation tasks")]
    private static partial void LogBackgroundTasksStarted(ILogger logger, int taskCount);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Background validation completed for item: {ItemLink}, Valid: {IsValid}")]
    private static partial void LogBackgroundValidationCompleted(ILogger logger, string itemLink, bool isValid);
}
