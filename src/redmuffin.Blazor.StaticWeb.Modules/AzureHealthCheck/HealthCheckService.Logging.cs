using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck;

internal sealed partial class HealthCheckService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to call hello endpoint")]
    private static partial void LogFailedToCallHelloEndpoint(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Hello endpoint returned a non-success status")]
    private static partial void LogHelloEndpointNonSuccess(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Hello endpoint returned an empty response")]
    private static partial void LogHelloEndpointEmptyResponse(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Hello endpoint call was cancelled")]
    private static partial void LogHelloEndpointCancelled(ILogger logger);
}
