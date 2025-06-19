using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common;

public class PrefixedLogger(ILogger logger, string prefix) : ILogger
{
	public IDisposable BeginScope<TState>(TState state) where TState : notnull => logger.BeginScope(state)!;

	public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		var message = $"{prefix}: {formatter(state, exception)}";
		logger.Log(logLevel, eventId, message, exception, (s, e) => message);
	}
}