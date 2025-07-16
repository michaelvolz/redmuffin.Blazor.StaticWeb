using System.Collections.Concurrent;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Integration;

public class MockCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _cache = new();

    public Task SetItemAsync<T>(string cacheNamespace, string key, T value, int? expirationMinutes = null, CancellationToken cancellationToken = default)
    {
        var namespaceDict = _cache.GetOrAdd(cacheNamespace, _ => new ConcurrentDictionary<string, object>());
        namespaceDict[key] = value!;
        return Task.CompletedTask;
    }

    public Task<T?> GetItemAsync<T>(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(cacheNamespace, out var namespaceDict) &&
            namespaceDict.TryGetValue(key, out var value))
            return Task.FromResult((T?)value);
        return Task.FromResult(default(T));
    }

    public Task RemoveItemAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(cacheNamespace, out var namespaceDict)) namespaceDict.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ContainsKeyAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        var contains = _cache.TryGetValue(cacheNamespace, out var namespaceDict) && namespaceDict.ContainsKey(key);
        return Task.FromResult(contains);
    }

    public Task<IEnumerable<string>> GetKeysAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        var keys = _cache.TryGetValue(cacheNamespace, out var namespaceDict)
            ? namespaceDict.Keys.AsEnumerable()
            : Enumerable.Empty<string>();
        return Task.FromResult(keys);
    }

    public Task ClearNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(cacheNamespace, out _);
        return Task.CompletedTask;
    }

    public Task<CacheNamespaceStats> GetNamespaceStatsAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        var stats = new CacheNamespaceStats
        {
            Namespace = cacheNamespace,
            TotalItems = _cache.TryGetValue(cacheNamespace, out var namespaceDict) ? namespaceDict.Count : 0,
            TotalSizeBytes = 0,
            ExpiredItemsCount = 0,
            OldestItemTimestamp = null,
            NewestItemTimestamp = null,
            AverageAccessCount = 0.0
        };
        return Task.FromResult(stats);
    }

    public Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new CacheStats
        {
            TotalItems = _cache.Values.Sum(d => d.Count),
            TotalSizeBytes = 0,
            QuotaLimitBytes = 5 * 1024 * 1024, // 5MB limit
            QuotaUsagePercent = 0.0,
            NamespaceCount = _cache.Count,
            NamespaceStats = new Dictionary<string, CacheNamespaceStats>(),
            TotalExpiredItemsCount = 0,
            OldestItemTimestamp = null,
            NewestItemTimestamp = null
        };
        return Task.FromResult(stats);
    }

    public Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<int> CleanupExpiredItemsAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}