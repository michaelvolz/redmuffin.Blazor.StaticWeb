namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Implementation of ICacheService that provides namespace separation for different data types.
/// </summary>
public class CacheService : ICacheService
{
    private const string NamespaceIndexKey = "cache_namespace_index";
    private readonly IBrowserStorageService _browserStorageService;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IBrowserStorageService browserStorageService, ILogger<CacheService> logger)
    {
        _browserStorageService = browserStorageService ?? throw new ArgumentNullException(nameof(browserStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetItemAsync<T>(string cacheNamespace, string key, T value, int? expirationMinutes = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var namespacedKey = GetNamespacedKey(cacheNamespace, key);
        var cacheEntry = new CacheEntry<T>
        {
            Value = value,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = expirationMinutes.HasValue ? DateTime.UtcNow.AddMinutes(expirationMinutes.Value) : null,
            AccessCount = 1,
            LastAccessedAt = DateTime.UtcNow
        };

        await _browserStorageService.SetItemAsync(namespacedKey, cacheEntry, cancellationToken);
        await UpdateNamespaceIndexAsync(cacheNamespace, key, cancellationToken);
    }

    public async Task<T?> GetItemAsync<T>(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var namespacedKey = GetNamespacedKey(cacheNamespace, key);
        var cacheEntry = await _browserStorageService.GetItemAsync<CacheEntry<T>>(namespacedKey, cancellationToken);

        if (cacheEntry == null) return default;

        // Check if the item has expired
        if (cacheEntry.ExpiresAt.HasValue && DateTime.UtcNow > cacheEntry.ExpiresAt.Value)
        {
            await RemoveItemAsync(cacheNamespace, key, cancellationToken);
            return default;
        }

        // Update access statistics
        cacheEntry.LastAccessedAt = DateTime.UtcNow;
        cacheEntry.AccessCount++;
        await _browserStorageService.SetItemAsync(namespacedKey, cacheEntry, cancellationToken);

        return cacheEntry.Value;
    }

    public async Task RemoveItemAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var namespacedKey = GetNamespacedKey(cacheNamespace, key);
        await _browserStorageService.RemoveItemAsync(namespacedKey, cancellationToken);
        await RemoveFromNamespaceIndexAsync(cacheNamespace, key, cancellationToken);
    }

    public async Task<bool> ContainsKeyAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var namespacedKey = GetNamespacedKey(cacheNamespace, key);
        return await _browserStorageService.ContainsKeyAsync(namespacedKey, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);

        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);
        return namespaceIndex.TryGetValue(cacheNamespace, out var keys) ? keys : Enumerable.Empty<string>();
    }

    public async Task ClearNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);

        var keys = await GetKeysAsync(cacheNamespace, cancellationToken);
        foreach (var key in keys)
        {
            var namespacedKey = GetNamespacedKey(cacheNamespace, key);
            await _browserStorageService.RemoveItemAsync(namespacedKey, cancellationToken);
        }

        await RemoveNamespaceFromIndexAsync(cacheNamespace, cancellationToken);
        _logger.LogInformation("Cleared cache namespace: {Namespace}", cacheNamespace);
    }

    public async Task<CacheNamespaceStats> GetNamespaceStatsAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);

        var keys = await GetKeysAsync(cacheNamespace, cancellationToken);
        var stats = new CacheNamespaceStats
        {
            Namespace = cacheNamespace,
            TotalItems = keys.Count()
        };

        var totalAccessCount = 0;
        foreach (var key in keys)
        {
            var namespacedKey = GetNamespacedKey(cacheNamespace, key);
            var size = await _browserStorageService.GetItemSizeAsync(namespacedKey, cancellationToken);
            stats.TotalSizeBytes += size;

            // Try to get metadata for timing information
            var cacheEntry = await _browserStorageService.GetItemAsync<CacheEntry<object>>(namespacedKey, cancellationToken);
            if (cacheEntry != null)
            {
                totalAccessCount += cacheEntry.AccessCount;

                if (cacheEntry.ExpiresAt.HasValue && DateTime.UtcNow > cacheEntry.ExpiresAt.Value) stats.ExpiredItemsCount++;

                if (stats.OldestItemTimestamp == null || cacheEntry.CachedAt < stats.OldestItemTimestamp) stats.OldestItemTimestamp = cacheEntry.CachedAt;

                if (stats.NewestItemTimestamp == null || cacheEntry.CachedAt > stats.NewestItemTimestamp) stats.NewestItemTimestamp = cacheEntry.CachedAt;
            }
        }

        stats.AverageAccessCount = stats.TotalItems > 0 ? (double)totalAccessCount / stats.TotalItems : 0;
        return stats;
    }

    public async Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);
        var stats = new CacheStats
        {
            NamespaceCount = namespaceIndex.Count,
            QuotaLimitBytes = _browserStorageService.GetQuotaLimit()
        };

        foreach (var namespaceName in namespaceIndex.Keys)
        {
            var namespaceStats = await GetNamespaceStatsAsync(namespaceName, cancellationToken);
            stats.NamespaceStats[namespaceName] = namespaceStats;
            stats.TotalItems += namespaceStats.TotalItems;
            stats.TotalSizeBytes += namespaceStats.TotalSizeBytes;
            stats.TotalExpiredItemsCount += namespaceStats.ExpiredItemsCount;

            if (stats.OldestItemTimestamp == null ||
                (namespaceStats.OldestItemTimestamp.HasValue && namespaceStats.OldestItemTimestamp < stats.OldestItemTimestamp))
                stats.OldestItemTimestamp = namespaceStats.OldestItemTimestamp;

            if (stats.NewestItemTimestamp == null ||
                (namespaceStats.NewestItemTimestamp.HasValue && namespaceStats.NewestItemTimestamp > stats.NewestItemTimestamp))
                stats.NewestItemTimestamp = namespaceStats.NewestItemTimestamp;
        }

        stats.QuotaUsagePercent = stats.QuotaLimitBytes > 0 ? (double)stats.TotalSizeBytes / stats.QuotaLimitBytes * 100 : 0;
        return stats;
    }

    public async Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);
        var totalCleanedUp = 0;

        foreach (var namespaceName in namespaceIndex.Keys)
        {
            var cleanedUp = await CleanupExpiredItemsAsync(namespaceName, cancellationToken);
            totalCleanedUp += cleanedUp;
        }

        _logger.LogInformation("Cleaned up {TotalCount} expired items across all namespaces", totalCleanedUp);
        return totalCleanedUp;
    }

    public async Task<int> CleanupExpiredItemsAsync(string cacheNamespace, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheNamespace);

        var keys = await GetKeysAsync(cacheNamespace, cancellationToken);
        var expiredKeys = new List<string>();

        foreach (var key in keys)
        {
            var namespacedKey = GetNamespacedKey(cacheNamespace, key);
            var cacheEntry = await _browserStorageService.GetItemAsync<CacheEntry<object>>(namespacedKey, cancellationToken);

            if (cacheEntry != null && cacheEntry.ExpiresAt.HasValue && DateTime.UtcNow > cacheEntry.ExpiresAt.Value) expiredKeys.Add(key);
        }

        foreach (var key in expiredKeys) await RemoveItemAsync(cacheNamespace, key, cancellationToken);

        if (expiredKeys.Count > 0) _logger.LogInformation("Cleaned up {Count} expired items from namespace: {Namespace}", expiredKeys.Count, cacheNamespace);

        return expiredKeys.Count;
    }

    private static string GetNamespacedKey(string cacheNamespace, string key)
    {
        return $"{cacheNamespace}:{key}";
    }

    private async Task UpdateNamespaceIndexAsync(string cacheNamespace, string key, CancellationToken cancellationToken)
    {
        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);

        if (!namespaceIndex.TryGetValue(cacheNamespace, out var keys))
        {
            keys = new HashSet<string>();
            namespaceIndex[cacheNamespace] = keys;
        }

        keys.Add(key);
        await _browserStorageService.SetItemAsync(NamespaceIndexKey, namespaceIndex, cancellationToken);
    }

    private async Task RemoveFromNamespaceIndexAsync(string cacheNamespace, string key, CancellationToken cancellationToken)
    {
        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);

        if (namespaceIndex.TryGetValue(cacheNamespace, out var keys))
        {
            keys.Remove(key);
            if (keys.Count == 0) namespaceIndex.Remove(cacheNamespace);
            await _browserStorageService.SetItemAsync(NamespaceIndexKey, namespaceIndex, cancellationToken);
        }
    }

    private async Task RemoveNamespaceFromIndexAsync(string cacheNamespace, CancellationToken cancellationToken)
    {
        var namespaceIndex = await GetNamespaceIndexAsync(cancellationToken);

        if (namespaceIndex.Remove(cacheNamespace)) await _browserStorageService.SetItemAsync(NamespaceIndexKey, namespaceIndex, cancellationToken);
    }

    private async Task<Dictionary<string, HashSet<string>>> GetNamespaceIndexAsync(CancellationToken cancellationToken)
    {
        return await _browserStorageService.GetItemAsync<Dictionary<string, HashSet<string>>>(NamespaceIndexKey, cancellationToken)
               ?? new Dictionary<string, HashSet<string>>();
    }
}