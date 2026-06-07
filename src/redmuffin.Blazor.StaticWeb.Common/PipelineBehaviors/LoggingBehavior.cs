using Mediator;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common.PipelineBehaviors;

public sealed partial class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        LogHandling(_logger, typeof(TMessage).Name);
        var response = await next(message, cancellationToken).ConfigureAwait(false);
        LogHandled(_logger, typeof(TMessage).Name);
        return response;
    }
}
