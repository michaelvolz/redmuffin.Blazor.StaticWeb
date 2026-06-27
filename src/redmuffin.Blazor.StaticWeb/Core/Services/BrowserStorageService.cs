using System.Security.Cryptography;
using System.Text;
using Blazored.LocalStorage;

namespace redmuffin.Blazor.StaticWeb.Core.Services;

public partial class BrowserStorageService(ILocalStorageService localStorage, ILogger<BrowserStorageService> logger) : IBrowserStorageService
{
    private const string IndexKey = "browser_storage_index";
    private const double EvictionThreshold = 0.85; // Start eviction when 85% full
    private const double EvictionTarget = 0.75; // Evict down to 75% full
    private static readonly TimeSpan DefaultExpirationTime = TimeSpan.FromDays(7); // 7 days cache expiration

    private long _quotaLimit = 1024 * 1024 * 10; // 10 MB default quota limit

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    ///     Determines whether a stored item is expired based on its metadata.
    /// </summary>
    /// <param name="metadata">The stored item metadata.</param>
    /// <param name="defaultExpiration">The default expiration time span for items without an explicit expiry.</param>
    /// <param name="utcNow">The current UTC time (injectable for testing).</param>
    /// <returns><c>true</c> if the item is expired; otherwise <c>false</c>.</returns>
    public static bool IsExpired(StoredItemMetadata metadata, TimeSpan defaultExpiration, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return metadata.ExpiresAt.HasValue
            ? now > metadata.ExpiresAt.Value
            : now - metadata.CreatedAt > defaultExpiration;
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
        await localStorage.SetItemAsync(hashedKey, value, cancellationToken).ConfigureAwait(false);
        await UpdateIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
        await EnsureQuotaAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);

        // Check if item is expired before retrieving
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.TryGetValue(hashedKey, out var metadata) && IsExpired(metadata, DefaultExpirationTime))
        {
            // Item is expired, remove it
            await localStorage.RemoveItemAsync(hashedKey, cancellationToken).ConfigureAwait(false);
            await RemoveFromIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
            return default;
        }

        var value = await localStorage.GetItemAsync<T?>(hashedKey, cancellationToken).ConfigureAwait(false);
        if (value != null) await UpdateIndexAsync(hashedKey, cancellationToken, true).ConfigureAwait(false);

        return value;
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        await localStorage.RemoveItemAsync(hashedKey, cancellationToken).ConfigureAwait(false);
        await RemoveFromIndexAsync(hashedKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        return await localStorage.ContainKeyAsync(hashedKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        return await localStorage.KeysAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await localStorage.ClearAsync(cancellationToken).ConfigureAwait(false);
        await localStorage.RemoveItemAsync(IndexKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the stats accumulator with a single storage entry.
    /// </summary>
    /// <param name="acc">The accumulator to update.</param>
    /// <param name="key">The storage key.</param>
    /// <param name="size">The size of the item in bytes.</param>
    /// <param name="index">The storage index for metadata lookups.</param>
    public static void UpdateAccumulator(StatsAccumulator acc, string key, long size, IReadOnlyDictionary<string, StoredItemMetadata> index)
    {
        acc.TotalSize += size;
        if (!index.TryGetValue(key, out var item)) return;

        if (IsExpired(item, DefaultExpirationTime)) acc.ExpiredCount++;
        if (acc.Oldest is null || item.CreatedAt < acc.Oldest) acc.Oldest = item.CreatedAt;
        if (acc.Newest is null || item.CreatedAt > acc.Newest) acc.Newest = item.CreatedAt;
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(cancellationToken).ConfigureAwait(false);
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        var sizes = await Task.WhenAll(keys.Select(k => GetItemSizeAsync(k, cancellationToken))).ConfigureAwait(false);

        var acc = keys.Zip(sizes, (k, s) => (Key: k, Size: s))
            .Aggregate(new StatsAccumulator(), (acc, entry) =>
            {
                UpdateAccumulator(acc, entry.Key, entry.Size, index);
                return acc;
            });

        return new StorageStats
        {
            TotalItems = await localStorage.LengthAsync(cancellationToken).ConfigureAwait(false),
            TotalSizeBytes = acc.TotalSize,
            QuotaLimitBytes = _quotaLimit,
            QuotaUsagePercent = (double)acc.TotalSize / _quotaLimit * 100,
            RecentlyAccessedCount = index.Count,
            ExpiredItemsCount = acc.ExpiredCount,
            OldestItemTimestamp = acc.Oldest,
            NewestItemTimestamp = acc.Newest
        };
    }

    public async Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default)
    {
        var item = await localStorage.GetItemAsync<string>(key, cancellationToken).ConfigureAwait(false);
        return item != null ? Encoding.UTF8.GetByteCount(item) : 0;
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

        var currentSize = sortedItems.Sum(item => item.Size);

        var candidatesToEvict = SelectItemsToEvict(sortedItems, currentSize, targetSizeBytes).ToList();
        foreach (var (key, size, lastAccessed) in candidatesToEvict)
        {
            await localStorage.RemoveItemAsync(key, cancellationToken).ConfigureAwait(false);
            await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
            evictedCount++;

            LogEvictedLRUItem(logger, key, size, lastAccessed);
        }

        if (evictedCount > 0)
        {
            var freedSize = sortedItems.Take(evictedCount).Sum(item => item.Size);
            LogLRUEvictionCompleted(logger, evictedCount, freedSize);
        }

        return evictedCount;
    }

    public static IEnumerable<(string Key, long Size, DateTime LastAccessed)> SelectItemsToEvict(
        IReadOnlyList<(string Key, long Size, DateTime LastAccessed)> sortedItems, long totalSize, long targetSizeBytes)
    {
        var remaining = totalSize;
        foreach (var item in sortedItems)
        {
            if (remaining <= targetSizeBytes)
                yield break;

            remaining -= item.Size;
            yield return item;
        }
    }

    public async Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        var expiredKeys = new List<string>();

        foreach (var (key, metadata) in index)
            if (IsExpired(metadata, DefaultExpirationTime))
                expiredKeys.Add(key);

        foreach (var key in expiredKeys)
        {
            await localStorage.RemoveItemAsync(key, cancellationToken).ConfigureAwait(false);
            await RemoveFromIndexAsync(key, cancellationToken).ConfigureAwait(false);
            LogRemovedExpiredItem(logger, key);
        }

        LogCleanedUpExpiredItems(logger, expiredKeys.Count);
        return expiredKeys.Count;
    }

    public async Task<int> ClearAllStorageAsync(CancellationToken cancellationToken = default)
    {
        // Get the current count of items before clearing
        var itemCount = await localStorage.LengthAsync(cancellationToken).ConfigureAwait(false);

        // Clear all localStorage data completely
        await localStorage.ClearAsync(cancellationToken).ConfigureAwait(false);

        // Log the operation
        LogAllStorageCleared(logger, itemCount);

        return itemCount;
    }

    private async Task EnsureQuotaAsync(CancellationToken cancellationToken = default)
    {
        var totalSize = await GetTotalSizeAsync(cancellationToken).ConfigureAwait(false);
        var quotaUsagePercent = (double)totalSize / _quotaLimit;

        // Check if we're approaching the storage limit
        if (quotaUsagePercent >= EvictionThreshold)
        {
            var targetSize = (long)(_quotaLimit * EvictionTarget);
            LogStorageApproachingCapacity(logger, quotaUsagePercent, EvictionTarget);

            // First, clean up expired items
            var expiredCount = await CleanupExpiredItemsAsync(cancellationToken).ConfigureAwait(false);

            // Recalculate size after cleanup
            totalSize = await GetTotalSizeAsync(cancellationToken).ConfigureAwait(false);

            // If still over the target, perform LRU eviction
            if (totalSize > targetSize)
            {
                var evictedCount = await EvictLeastRecentlyUsedAsync(targetSize, cancellationToken).ConfigureAwait(false);
                LogStorageOptimizationCompleted(logger, expiredCount, evictedCount);
            }
            else
            {
                LogStorageOptimizationExpiredOnly(logger, expiredCount);
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

        await localStorage.SetItemAsync(IndexKey, index, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveFromIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.Remove(key)) await localStorage.SetItemAsync(IndexKey, index, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, StoredItemMetadata>> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        return await localStorage.GetItemAsync<Dictionary<string, StoredItemMetadata>>(IndexKey, cancellationToken).ConfigureAwait(false) ??
                new Dictionary<string, StoredItemMetadata>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Mutable accumulator for computing storage statistics via <see cref="Enumerable.Aggregate{TSource,TAccumulate}" />.
    /// </summary>
    public sealed class StatsAccumulator
    {
        public long TotalSize { get; set; }
        public int ExpiredCount { get; set; }
        public DateTime? Oldest { get; set; }
        public DateTime? Newest { get; set; }
    }
}