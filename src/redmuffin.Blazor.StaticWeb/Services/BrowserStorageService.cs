using System.Security.Cryptography;
using System.Text;
using Blazored.LocalStorage;

namespace redmuffin.Blazor.StaticWeb.Services;

public class BrowserStorageService : IBrowserStorageService
{
    private const string IndexKey = "browser_storage_index";
    private const double EvictionThreshold = 0.85; // Start eviction when 85% full
    private const double EvictionTarget = 0.75; // Evict down to 75% full
    private static readonly TimeSpan DefaultExpirationTime = TimeSpan.FromDays(7); // 7 days cache expiration
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<BrowserStorageService> _logger;
    private long _quotaLimit = 1024 * 1024 * 10; // 10 MB default quota limit

    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, string, long, DateTime, Exception?> LogEvictedLRUItem =
        LoggerMessage.Define<string, long, DateTime>(LogLevel.Debug, new EventId(1, nameof(LogEvictedLRUItem)),
            "Evicted LRU item: {Key}, Size: {Size} bytes, LastAccessed: {LastAccessed}");
    private static readonly Action<ILogger, int, long, Exception?> LogLRUEvictionCompleted =
        LoggerMessage.Define<int, long>(LogLevel.Information, new EventId(2, nameof(LogLRUEvictionCompleted)),
            "LRU eviction completed. Evicted {Count} items, freed {FreedSize} bytes");
    private static readonly Action<ILogger, string, Exception?> LogRemovedExpiredItem =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, nameof(LogRemovedExpiredItem)),
            "Removed expired cache item: {Key}");
    private static readonly Action<ILogger, int, Exception?> LogCleanedUpExpiredItems =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(4, nameof(LogCleanedUpExpiredItems)),
            "Cleaned up {ExpiredCount} expired cache items");
    private static readonly Action<ILogger, double, double, Exception?> LogStorageApproachingCapacity =
        LoggerMessage.Define<double, double>(LogLevel.Information, new EventId(5, nameof(LogStorageApproachingCapacity)),
            "Storage approaching capacity ({Usage:P2}), starting LRU eviction to {Target:P2}");
    private static readonly Action<ILogger, int, int, Exception?> LogStorageOptimizationCompleted =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(6, nameof(LogStorageOptimizationCompleted)),
            "Storage optimization completed. Expired: {ExpiredCount}, Evicted: {EvictedCount}");
    private static readonly Action<ILogger, int, Exception?> LogStorageOptimizationExpiredOnly =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(7, nameof(LogStorageOptimizationExpiredOnly)),
            "Storage optimization completed using only expired item cleanup: {ExpiredCount}");

    public BrowserStorageService(ILocalStorageService localStorage, ILogger<BrowserStorageService> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public void SetQuotaLimit(long quotaBytes)
    {
        _quotaLimit = quotaBytes;
    }

    public long GetQuotaLimit()
    {
        return _quotaLimit;
    }

    public async Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        await _localStorage.SetItemAsync(hashedKey, value, cancellationToken).ConfigureAwait(false);
        await UpdateIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
        await EnsureQuotaAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);

        // Check if item is expired before retrieving
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.TryGetValue(hashedKey, out var metadata))
        {
            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
            {
                // Item is expired, remove it
                await _localStorage.RemoveItemAsync(hashedKey, cancellationToken).ConfigureAwait(false);
                await RemoveFromIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
                return default;
            }

            if (!metadata.ExpiresAt.HasValue && IsExpired(metadata.CreatedAt, DefaultExpirationTime))
            {
                // Legacy item is expired, remove it
                await _localStorage.RemoveItemAsync(hashedKey, cancellationToken).ConfigureAwait(false);
                await RemoveFromIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
                return default;
            }
        }

        var value = await _localStorage.GetItemAsync<T?>(hashedKey, cancellationToken).ConfigureAwait(false);
        if (value != null) await UpdateIndexAsync(hashedKey, cancellationToken, true).ConfigureAwait(false);

        return value;
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        await _localStorage.RemoveItemAsync(hashedKey, cancellationToken).ConfigureAwait(false);
        await RemoveFromIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        return await _localStorage.ContainKeyAsync(hashedKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _localStorage.KeysAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _localStorage.ClearAsync(cancellationToken).ConfigureAwait(false);
        await _localStorage.RemoveItemAsync(IndexKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(cancellationToken).ConfigureAwait(false);
        long totalSize = 0;
        var expiredCount = 0;
        DateTime? oldest = null;
        DateTime? newest = null;

        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        foreach (var key in keys)
        {
            var size = await GetItemSizeAsync(key, cancellationToken).ConfigureAwait(false);
            totalSize += size;

            if (index.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt.HasValue && DateTime.UtcNow > item.ExpiresAt.Value)
                    expiredCount++;
                else if (!item.ExpiresAt.HasValue && IsExpired(item.CreatedAt, DefaultExpirationTime)) expiredCount++;

                if (oldest == null || item.CreatedAt < oldest) oldest = item.CreatedAt;
                if (newest == null || item.CreatedAt > newest) newest = item.CreatedAt;
            }
        }

        return new StorageStats
        {
            TotalItems = await _localStorage.LengthAsync(cancellationToken).ConfigureAwait(false),
            TotalSizeBytes = totalSize,
            QuotaLimitBytes = _quotaLimit,
            QuotaUsagePercent = (double)totalSize / _quotaLimit * 100,
            RecentlyAccessedCount = (await GetIndexAsync(cancellationToken).ConfigureAwait(false)).Count,
            ExpiredItemsCount = expiredCount,
            OldestItemTimestamp = oldest,
            NewestItemTimestamp = newest
        };
    }

    public async Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default)
    {
        var item = await _localStorage.GetItemAsync<string>(key, cancellationToken).ConfigureAwait(false);
        if (item != null) return Encoding.UTF8.GetByteCount(item);

        return 0;
    }

    public async Task<int> EvictLeastRecentlyUsedAsync(long targetSizeBytes, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        var evictedCount = 0;

        if (index.Count == 0) return evictedCount;

        // Get all items with their sizes, sorted by LRU order
        var itemsWithSizes = new List<(string Key, long Size, DateTime LastAccessed)>();
        foreach (var kvp in index)
        {
            var size = await GetItemSizeAsync(kvp.Key, cancellationToken).ConfigureAwait(false);
            var lastAccessed = kvp.Value.LastAccessed == default ? kvp.Value.CreatedAt : kvp.Value.LastAccessed;
            itemsWithSizes.Add((kvp.Key, size, lastAccessed));
        }

        // Sort by last accessed time (oldest first)
        var sortedItems = itemsWithSizes.OrderBy(item => item.LastAccessed).ToList();

        // Calculate current total size
        var currentSize = sortedItems.Sum(item => item.Size);

        // Evict items until we reach the target size
        foreach (var item in sortedItems)
        {
            if (currentSize <= targetSizeBytes) break;

            await _localStorage.RemoveItemAsync(item.Key, cancellationToken).ConfigureAwait(false);
            await RemoveFromIndexAsync(item.Key, cancellationToken).ConfigureAwait(false);
            currentSize -= item.Size;
            evictedCount++;

            LogEvictedLRUItem(_logger, item.Key, item.Size, item.LastAccessed, null);
        }

        if (evictedCount > 0)
            LogLRUEvictionCompleted(_logger, evictedCount, sortedItems.Take(evictedCount).Sum(item => item.Size), null);

        return evictedCount;
    }

    public async Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        var expiredKeys = new List<string>();

        foreach (var kvp in index)
        {
            var metadata = kvp.Value;
            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
                expiredKeys.Add(kvp.Key);
            else if (!metadata.ExpiresAt.HasValue && IsExpired(metadata.CreatedAt, DefaultExpirationTime))
                // Handle legacy items without ExpiresAt
                expiredKeys.Add(kvp.Key);
        }

        foreach (var key in expiredKeys)
        {
            await _localStorage.RemoveItemAsync(key, cancellationToken).ConfigureAwait(false);
            await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
            LogRemovedExpiredItem(_logger, key, null);
        }

        LogCleanedUpExpiredItems(_logger, expiredKeys.Count, null);
        return expiredKeys.Count;
    }

    private async Task EnsureQuotaAsync(CancellationToken cancellationToken = default)
    {
        var totalSize = await GetTotalSizeAsync(cancellationToken).ConfigureAwait(false);
        var quotaUsagePercent = (double)totalSize / _quotaLimit;

        // Check if we're approaching the storage limit
        if (quotaUsagePercent >= EvictionThreshold)
        {
            var targetSize = (long)(_quotaLimit * EvictionTarget);
            LogStorageApproachingCapacity(_logger, quotaUsagePercent, EvictionTarget, null);

            // First, clean up expired items
            var expiredCount = await CleanupExpiredItemsAsync(cancellationToken).ConfigureAwait(false);

            // Recalculate size after cleanup
            totalSize = await GetTotalSizeAsync(cancellationToken).ConfigureAwait(false);

            // If still over the target, perform LRU eviction
            if (totalSize > targetSize)
            {
                var evictedCount = await EvictLeastRecentlyUsedAsync(targetSize, cancellationToken).ConfigureAwait(false);
                LogStorageOptimizationCompleted(_logger, expiredCount, evictedCount, null);
            }
            else
            {
                LogStorageOptimizationExpiredOnly(_logger, expiredCount, null);
            }
        }
    }

    private async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(cancellationToken).ConfigureAwait(false);
        long totalSize = 0;
        foreach (var key in keys) totalSize += await GetItemSizeAsync(key, cancellationToken).ConfigureAwait(false);

        return totalSize;
    }

    private async Task UpdateIndexAsync(string key, CancellationToken cancellationToken, bool updateAccess = false)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        if (!index.ContainsKey(key))
            index[key] = new StoredItemMetadata
            {
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(DefaultExpirationTime)
            };

        if (updateAccess)
            index[key].LastAccessed = DateTime.UtcNow;
        else if (index[key].LastAccessed == default)
            // Initialize LastAccessed to CreatedAt if not set
            index[key].LastAccessed = index[key].CreatedAt;

        await _localStorage.SetItemAsync(IndexKey, index, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveFromIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.Remove(key)) await _localStorage.SetItemAsync(IndexKey, index, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, StoredItemMetadata>> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        return await _localStorage.GetItemAsync<Dictionary<string, StoredItemMetadata>>(IndexKey, cancellationToken).ConfigureAwait(false) ??
               new Dictionary<string, StoredItemMetadata>(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetMetadataKey(string key)
    {
        return key + "_meta";
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static bool IsExpired(DateTime createdAt, TimeSpan expirationTime)
    {
        return DateTime.UtcNow - createdAt > expirationTime;
    }
}