using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

/// <summary>
///     Logging partial class for <see cref="RaindropJsonDeserializer" />.
/// </summary>
public static partial class RaindropJsonDeserializer
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Attempting JSON deserialization for {Source} using {Strategy}"
    )]
    private static partial void LogAttemptingDeserialization(
        ILogger logger,
        string source,
        string strategy
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "JSON deserialization successful for {Source} using {Strategy}"
    )]
    private static partial void LogDeserializationSuccess(
        ILogger logger,
        string source,
        string strategy
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "JSON deserialization attempt failed for {Source} using {Strategy}"
    )]
    private static partial void LogDeserializationAttemptFailed(
        ILogger logger,
        Exception exception,
        string source,
        string strategy
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "All JSON deserialization strategies failed for {Source}"
    )]
    private static partial void LogAllDeserializationStrategiesFailed(
        ILogger logger,
        string source
    );
}
