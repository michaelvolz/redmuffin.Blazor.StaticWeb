using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Common;

/// <summary>
///     High-performance logger wrapper that adds a prefix to all log messages.
///     Optimized to avoid unnecessary string allocations and delegate closures.
/// </summary>
public sealed class PrefixedLogger(ILogger logger, string prefix) : ILogger
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Early exit if logging is disabled for this level
        if (!_logger.IsEnabled(logLevel)) return;

        ArgumentNullException.ThrowIfNull(formatter);

        // Use a static formatter to avoid closure allocation
        _logger.Log(logLevel, eventId, new PrefixedState<TState>(_prefix, state), exception, PrefixedFormatter<TState>.Instance);
    }

    /// <summary>
    ///     Wrapper struct to hold prefix and original state without allocating a closure.
    /// </summary>
    private readonly struct PrefixedState<TState>(string prefix, TState state)
    {
        public readonly string Prefix = prefix;
        public readonly TState State = state;
    }

    /// <summary>
    ///     Static formatter to avoid delegate allocation on every log call.
    /// </summary>
    private static class PrefixedFormatter<TState>
    {
        public static readonly Func<PrefixedState<TState>, Exception?, string> Instance = Format;

        private static string Format(PrefixedState<TState> state, Exception? exception)
        {
            // Use string interpolation for better performance than concatenation
            var originalMessage = state.State?.ToString() ?? string.Empty;
            return $"{state.Prefix}: {originalMessage}";
        }
    }
}