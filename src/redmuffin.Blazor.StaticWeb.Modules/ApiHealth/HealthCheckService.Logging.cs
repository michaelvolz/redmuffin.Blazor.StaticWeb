using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public sealed partial class HealthCheckService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to call hello endpoint")]
    private static partial void LogFailedToCallHelloEndpoint(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Hello endpoint call was cancelled")]
    private static partial void LogHelloEndpointCancelled(ILogger logger);
}
