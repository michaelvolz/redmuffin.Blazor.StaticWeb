namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Logging partial class for ImageValidationCacheService containing LoggerMessage delegates.
/// </summary>
public sealed partial class ImageValidationCacheService
{
    /// <summary>
    ///     LoggerMessage delegate for background validation failures.
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> LogBackgroundValidationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogBackgroundValidationFailed)),
            "Background validation failed for item: {ItemLink}");

    /// <summary>
    ///     LoggerMessage delegate for background tasks started.
    /// </summary>
    private static readonly Action<ILogger, int, Exception?> LogBackgroundTasksStarted =
        LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(2, nameof(LogBackgroundTasksStarted)),
            "Started {TaskCount} background validation tasks");

    /// <summary>
    ///     LoggerMessage delegate for background validation completion.
    /// </summary>
    private static readonly Action<ILogger, string, bool, Exception?> LogBackgroundValidationCompleted =
        LoggerMessage.Define<string, bool>(
            LogLevel.Debug,
            new EventId(3, nameof(LogBackgroundValidationCompleted)),
            "Background validation completed for item: {ItemLink}, Valid: {IsValid}");
}