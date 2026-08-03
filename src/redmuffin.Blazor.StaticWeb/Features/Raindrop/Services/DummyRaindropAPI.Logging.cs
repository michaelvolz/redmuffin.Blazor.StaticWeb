namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
///     Logging partial class for DummyRaindropAPI containing LoggerMessage delegates.
/// </summary>
public sealed partial class DummyRaindropAPI
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Loading videos from dummy data source")]
    private static partial void LogLoadingVideos(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {Count} videos from dummy data")]
    private static partial void LogVideosLoaded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading articles from dummy data source")]
    private static partial void LogLoadingArticles(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {Count} articles from dummy data")]
    private static partial void LogArticlesLoaded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading JSON file: {FilePath}")]
    private static partial void LogLoadingFile(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded JSON file: {FilePath}, size: {ContentLength} characters")]
    private static partial void LogFileLoaded(ILogger logger, string filePath, int contentLength);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load JSON file: {FileName}")]
    private static partial void LogFileLoadError(ILogger logger, Exception exception, string fileName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse JSON content from file: {FileName}")]
    private static partial void LogJsonParseError(ILogger logger, Exception exception, string fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Operation {OperationName} was cancelled")]
    private static partial void LogOperationCancelled(ILogger logger, Exception exception, string operationName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in operation: {OperationName}")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception, string operationName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Disposing DummyRaindropAPI instance")]
    private static partial void LogDisposing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Attempting JSON deserialization for {FileName} using {Strategy}")]
    private static partial void LogAttemptingDeserialization(ILogger logger, string fileName, string strategy);

    [LoggerMessage(Level = LogLevel.Debug, Message = "JSON deserialization successful for {FileName} using {Strategy}")]
    private static partial void LogDeserializationSuccess(ILogger logger, string fileName, string strategy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "JSON deserialization attempt failed for {FileName} using {Strategy}")]
    private static partial void LogDeserializationAttemptFailed(ILogger logger, Exception exception, string fileName, string strategy);

    [LoggerMessage(Level = LogLevel.Error, Message = "All JSON deserialization strategies failed for {FileName}")]
    private static partial void LogAllDeserializationStrategiesFailed(ILogger logger, string fileName);
}