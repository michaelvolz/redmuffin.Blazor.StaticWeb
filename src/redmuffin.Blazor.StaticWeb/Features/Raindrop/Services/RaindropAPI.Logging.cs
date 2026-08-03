namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
///     Logging partial class for RaindropAPI containing LoggerMessage delegates.
/// </summary>
public sealed partial class RaindropAPI
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Calling videos API endpoint")]
    private static partial void LogCallingVideosAPI(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {Count} videos from API")]
    private static partial void LogVideosLoaded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling articles API endpoint")]
    private static partial void LogCallingArticlesAPI(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully loaded {Count} articles from API")]
    private static partial void LogArticlesLoaded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "API call failed for {Operation} with status code {StatusCode}: {ReasonPhrase}")]
    private static partial void LogAPICallFailed(ILogger logger, string operation, int statusCode, string reasonPhrase);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Empty response received from {Operation}")]
    private static partial void LogEmptyAPIResponse(ILogger logger, string operation);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP request error in {Operation}")]
    private static partial void LogAPIRequestError(ILogger logger, Exception exception, string operation);

    [LoggerMessage(Level = LogLevel.Error, Message = "JSON parsing error in {Operation}")]
    private static partial void LogJsonParseError(ILogger logger, Exception exception, string operation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Operation {OperationName} was cancelled")]
    private static partial void LogOperationCancelled(ILogger logger, Exception exception, string operationName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in operation: {OperationName}")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception, string operationName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Attempting JSON deserialization for {Source} using {Strategy}")]
    private static partial void LogAttemptingDeserialization(ILogger logger, string source, string strategy);

    [LoggerMessage(Level = LogLevel.Debug, Message = "JSON deserialization successful for {Source} using {Strategy}")]
    private static partial void LogDeserializationSuccess(ILogger logger, string source, string strategy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "JSON deserialization attempt failed for {Source} using {Strategy}")]
    private static partial void LogDeserializationAttemptFailed(ILogger logger, Exception exception, string source, string strategy);

    [LoggerMessage(Level = LogLevel.Error, Message = "All JSON deserialization strategies failed for {Source}")]
    private static partial void LogAllDeserializationStrategiesFailed(ILogger logger, string source);

    [LoggerMessage(Level = LogLevel.Information, Message = "Disposing RaindropAPI instance")]
    private static partial void LogDisposing(ILogger logger);
}