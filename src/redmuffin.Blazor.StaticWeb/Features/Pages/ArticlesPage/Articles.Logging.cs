namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage;

/// <summary>
/// Logging partial class for Articles component containing LoggerMessage delegates.
/// </summary>
public partial class Articles
{
    /// <summary>
    /// LoggerMessage delegate for shimmer errors.
    /// </summary>
    private static readonly Action<ILogger, string, Exception> LogShimmerError =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogShimmerError)),
            "Error stopping shimmer for element: {ElementId}");

    /// <summary>
    /// LoggerMessage delegate for background validation failures.
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> LogBackgroundValidationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogBackgroundValidationFailed)),
            "Background validation failed for article: {ArticleLink}");

    /// <summary>
    /// LoggerMessage delegate for background tasks started.
    /// </summary>
    private static readonly Action<ILogger, int, Exception?> LogBackgroundTasksStarted =
        LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3, nameof(LogBackgroundTasksStarted)),
            "Started {TaskCount} background validation tasks");

    /// <summary>
    /// LoggerMessage delegate for background validation completion.
    /// </summary>
    private static readonly Action<ILogger, string, bool, Exception?> LogBackgroundValidationCompleted =
        LoggerMessage.Define<string, bool>(
            LogLevel.Debug,
            new EventId(4, nameof(LogBackgroundValidationCompleted)),
            "Background validation completed for article: {ArticleLink}, Valid: {IsValid}");

    /// <summary>
    /// LoggerMessage delegate for image load handling errors.
    /// </summary>
    private static readonly Action<ILogger, string, Exception> LogImageLoadHandlingError =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5, nameof(LogImageLoadHandlingError)),
            "Error handling image load for article: {ArticleLink}");
}