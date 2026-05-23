using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Presentation;

[Category("Feature:Raindrop")]
[Category("Unit")]
public sealed partial class RaindropPageOrchestratorTests
{
}

/// <summary>
///     Fake cache for orchestrator tests — only GetAsync and SetAsync are used.
/// </summary>
public sealed class RaindropItemsCache_Fake : IRaindropItemsCache
{
    public RaindropCacheResult<IList<RaindropItem>>? GetResult { get; set; }
    public Exception? GetException { get; set; }
    public List<RaindropItem>? LastSetItems { get; private set; }

    public Task<RaindropCacheResult<IList<RaindropItem>>> GetAsync(string cacheType, CancellationToken cancellationToken = default)
    {
        if (GetException is not null)
            return Task.FromException<RaindropCacheResult<IList<RaindropItem>>>(GetException);
            return Task.FromResult(GetResult ?? new RaindropCacheResult<IList<RaindropItem>>());
    }

    public Task SetAsync(string cacheType, IList<RaindropItem> items, CancellationToken cancellationToken = default)
    {
        LastSetItems = items.ToList();
        return Task.CompletedTask;
    }

    public Task ClearAsync(string cacheType, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> IsExpiredAsync(string cacheType, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>
///     Spy logger for verifying log output.
/// </summary>
public sealed class Logger_Spy : ILogger
{
    public List<LogEntry> LogEntries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }
}

public record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
