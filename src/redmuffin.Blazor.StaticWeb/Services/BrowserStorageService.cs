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
        await _localStorage.SetItemAsync(hashedKey, value, cancellationToken);
        await UpdateIndexAsync(hashedKey, cancellationToken);
        await EnsureQuotaAsync(cancellationToken);
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);

        // Check if item is expired before retrieving
        var index = await GetIndexAsync(cancellationToken);
        if (index.ContainsKey(hashedKey))
        {
            var metadata = index[hashedKey];
            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
            {
                // Item is expired, remove it
                await _localStorage.RemoveItemAsync(hashedKey, cancellationToken);
                await RemoveFromIndexAsync(hashedKey, cancellationToken);
                return default;
            }

            if (!metadata.ExpiresAt.HasValue && IsExpired(metadata.CreatedAt, DefaultExpirationTime))
            {
                // Legacy item is expired, remove it
                await _localStorage.RemoveItemAsync(hashedKey, cancellationToken);
                await RemoveFromIndexAsync(hashedKey, cancellationToken);
                return default;
            }
        }

        var value = await _localStorage.GetItemAsync<T?>(hashedKey, cancellationToken);
        if (value != null) await UpdateIndexAsync(hashedKey, cancellationToken, true);

        return value;
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        await _localStorage.RemoveItemAsync(hashedKey, cancellationToken);
        await RemoveFromIndexAsync(hashedKey, cancellationToken);
    }

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeHash(key);
        return await _localStorage.ContainKeyAsync(hashedKey, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _localStorage.KeysAsync();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _localStorage.ClearAsync(cancellationToken);
        await _localStorage.RemoveItemAsync(IndexKey, cancellationToken);
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(cancellationToken);
        long totalSize = 0;
        var expiredCount = 0;
        DateTime? oldest = null;
        DateTime? newest = null;

        var index = await GetIndexAsync(cancellationToken);
        foreach (var key in keys)
        {
            var size = await GetItemSizeAsync(key, cancellationToken);
            totalSize += size;

            if (index.ContainsKey(key))
            {
                var item = index[key];
                if (item.ExpiresAt.HasValue && DateTime.UtcNow > item.ExpiresAt.Value)
                    expiredCount++;
                else if (!item.ExpiresAt.HasValue && IsExpired(item.CreatedAt, DefaultExpirationTime)) expiredCount++;

                if (oldest == null || item.CreatedAt < oldest) oldest = item.CreatedAt;
                if (newest == null || item.CreatedAt > newest) newest = item.CreatedAt;
            }
        }

        return new StorageStats
        {
            TotalItems = await _localStorage.LengthAsync(cancellationToken),
            TotalSizeBytes = totalSize,
            QuotaLimitBytes = _quotaLimit,
            QuotaUsagePercent = (double)totalSize / _quotaLimit * 100,
            RecentlyAccessedCount = (await GetIndexAsync(cancellationToken)).Count,
            ExpiredItemsCount = expiredCount,
            OldestItemTimestamp = oldest,
            NewestItemTimestamp = newest
        };
    }

    public async Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default)
    {
        var item = await _localStorage.GetItemAsync<string>(key, cancellationToken);
        if (item != null) return Encoding.UTF8.GetByteCount(item);

        return 0;
    }

    public async Task<int> EvictLeastRecentlyUsedAsync(long targetSizeBytes, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        var evictedCount = 0;

        if (!index.Any()) return evictedCount;

        // Get all items with their sizes, sorted by LRU order
        var itemsWithSizes = new List<(string key, long size, DateTime lastAccessed)>();
        foreach (var kvp in index)
        {
            var size = await GetItemSizeAsync(kvp.Key, cancellationToken);
            var lastAccessed = kvp.Value.LastAccessed == default ? kvp.Value.CreatedAt : kvp.Value.LastAccessed;
            itemsWithSizes.Add((kvp.Key, size, lastAccessed));
        }

        // Sort by last accessed time (oldest first)
        var sortedItems = itemsWithSizes.OrderBy(item => item.lastAccessed).ToList();

        // Calculate current total size
        var currentSize = sortedItems.Sum(item => item.size);

        // Evict items until we reach the target size
        foreach (var item in sortedItems)
        {
            if (currentSize <= targetSizeBytes) break;

            await _localStorage.RemoveItemAsync(item.key, cancellationToken);
            await RemoveFromIndexAsync(item.key, cancellationToken);
            currentSize -= item.size;
            evictedCount++;

            _logger.LogDebug("Evicted LRU item: {Key}, Size: {Size} bytes, LastAccessed: {LastAccessed}",
                item.key, item.size, item.lastAccessed);
        }

        if (evictedCount > 0)
            _logger.LogInformation("LRU eviction completed. Evicted {Count} items, freed {FreedSize} bytes",
                evictedCount, sortedItems.Take(evictedCount).Sum(item => item.size));

        return evictedCount;
    }

    public async Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
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
            await _localStorage.RemoveItemAsync(key, cancellationToken);
            await RemoveFromIndexAsync(key, cancellationToken);
            _logger.LogDebug("Removed expired cache item: {Key}", key);
        }

        _logger.LogInformation("Cleaned up {ExpiredCount} expired cache items", expiredKeys.Count);
        return expiredKeys.Count;
    }

    private async Task EnsureQuotaAsync(CancellationToken cancellationToken = default)
    {
        var totalSize = await GetTotalSizeAsync(cancellationToken);
        var quotaUsagePercent = (double)totalSize / _quotaLimit;

        // Check if we're approaching the storage limit
        if (quotaUsagePercent >= EvictionThreshold)
        {
            var targetSize = (long)(_quotaLimit * EvictionTarget);
            _logger.LogInformation("Storage approaching capacity ({Usage:P2}), starting LRU eviction to {Target:P2}",
                quotaUsagePercent, EvictionTarget);

            // First, clean up expired items
            var expiredCount = await CleanupExpiredItemsAsync(cancellationToken);

            // Recalculate size after cleanup
            totalSize = await GetTotalSizeAsync(cancellationToken);

            // If still over the target, perform LRU eviction
            if (totalSize > targetSize)
            {
                var evictedCount = await EvictLeastRecentlyUsedAsync(targetSize, cancellationToken);
                _logger.LogInformation("Storage optimization completed. Expired: {ExpiredCount}, Evicted: {EvictedCount}",
                    expiredCount, evictedCount);
            }
            else
            {
                _logger.LogInformation("Storage optimization completed using only expired item cleanup: {ExpiredCount}",
                    expiredCount);
            }
        }
    }

    private async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(cancellationToken);
        long totalSize = 0;
        foreach (var key in keys) totalSize += await GetItemSizeAsync(key, cancellationToken);

        return totalSize;
    }

    private async Task UpdateIndexAsync(string key, CancellationToken cancellationToken, bool updateAccess = false)
    {
        var index = await GetIndexAsync(cancellationToken);
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

        await _localStorage.SetItemAsync(IndexKey, index, cancellationToken);
    }

    private async Task RemoveFromIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        if (index.Remove(key)) await _localStorage.SetItemAsync(IndexKey, index, cancellationToken);
    }

    private async Task<Dictionary<string, StoredItemMetadata>> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        return await _localStorage.GetItemAsync<Dictionary<string, StoredItemMetadata>>(IndexKey, cancellationToken) ??
               new Dictionary<string, StoredItemMetadata>();
    }

    private string GetMetadataKey(string key)
    {
        return key + "_meta";
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private bool IsExpired(DateTime createdAt, TimeSpan expirationTime)
    {
        return DateTime.UtcNow - createdAt > expirationTime;
    }
}