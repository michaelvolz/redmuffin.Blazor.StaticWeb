using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
/// Logging partial class for RaindropAPIFactory containing LoggerMessage delegates.
/// </summary>
public sealed partial class RaindropAPIFactory
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Creating DummyRaindropAPI for base URI: {BaseUri}")]
    private static partial void LogCreatingDummyAPI(ILogger logger, string baseUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating RaindropAPI for base URI: {BaseUri}")]
    private static partial void LogCreatingRealAPI(ILogger logger, string baseUri);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create IRaindropAPI instance for base URI: {BaseUri}")]
    private static partial void LogFactoryError(ILogger logger, Exception exception, string baseUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment detection for base URI: {BaseUri}, using dummy data: {IsDummyEnvironment}")]
    private static partial void LogEnvironmentDetection(ILogger logger, string baseUri, bool isDummyEnvironment);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid base URI detected: {BaseUri}")]
    private static partial void LogInvalidBaseUri(ILogger logger, string? baseUri);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to detect environment for base URI: {BaseUri}")]
    private static partial void LogEnvironmentDetectionError(ILogger logger, Exception exception, string baseUri);
}