using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Services;

/// <summary>
///     Helper classes and methods for CacheMonitoringServiceTests.
/// </summary>
[Category("Feature:Services")]
public sealed partial class CacheMonitoringServiceTests
{
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    public sealed class TestScope : IDisposable
    {
        private bool _disposed;

        public TestScope()
        {
            BrowserStorageService = new BrowserStorageService_Fake();
            Logger = new Logger_Spy<CacheMonitoringService>();
            Service = new CacheMonitoringService(BrowserStorageService, Logger);
        }

        public BrowserStorageService_Fake BrowserStorageService { get; }

        public Logger_Spy<CacheMonitoringService> Logger { get; }

        public CacheMonitoringService Service { get; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    public sealed class BrowserStorageService_Fake : IBrowserStorageService
    {
        private readonly Queue<StorageStats> _storageStatsResponses = [];

        public Exception? GetStorageStatsException { get; set; }

        public long? LastEvictionTargetSize { get; private set; }

        public int EvictionResult { get; set; }

        public void QueueStorageStats(params StorageStats[] stats)
        {
            foreach (var item in stats) _storageStatsResponses.Enqueue(item);
        }

        public Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
        {
            if (GetStorageStatsException is not null)
                return Task.FromException<StorageStats>(GetStorageStatsException);

            if (_storageStatsResponses.Count > 0)
                return Task.FromResult(_storageStatsResponses.Dequeue());

            return Task.FromResult(new StorageStats());
        }

        public Task<int> EvictLeastRecentlyUsedAsync(long targetSizeBytes, CancellationToken cancellationToken = default)
        {
            LastEvictionTargetSize = targetSizeBytes;
            return Task.FromResult(EvictionResult);
        }

        public Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RemoveItemAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ClearAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void SetQuotaLimit(long quotaBytes) => throw new NotSupportedException();

        public long GetQuotaLimit() => throw new NotSupportedException();

        public Task<int> ClearAllStorageAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public sealed class Logger_Spy<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
