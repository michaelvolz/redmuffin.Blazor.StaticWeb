using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common.PipelineBehaviors;

public sealed partial class LoggingBehavior<TMessage, TResponse>
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Handling {MessageType}")]
    private static partial void LogHandling(ILogger logger, string messageType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Handled {MessageType}")]
    private static partial void LogHandled(ILogger logger, string messageType);
}
