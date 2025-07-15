namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
/// Service for cache management with namespace separation for different data types.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Stores an item in the specified cache namespace.
    /// </summary>
    /// <typeparam name="T">Type of the item to store</typeparam>
    /// <param name="cacheNamespace">Cache namespace to separate different types of data</param>
    /// <param name="key">Storage key</param>
    /// <param name="value">Value to store</param>
    /// <param name="expirationMinutes">Expiration time in minutes (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetItemAsync<T>(string cacheNamespace, string key, T value, int? expirationMinutes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an item from the specified cache namespace.
    /// </summary>
    /// <typeparam name="T">Type of the item to retrieve</typeparam>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored item or default value if not found</returns>
    Task<T?> GetItemAsync<T>(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveItemAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an item exists in the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the item exists, false otherwise</returns>
    Task<bool> ContainsKeyAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all keys in the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of keys in the namespace</returns>
    Task<IEnumerable<string>> GetKeysAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items from the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics for the specified namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache statistics for the namespace</returns>
    Task<CacheNamespaceStats> GetNamespaceStatsAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overall cache statistics across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall cache statistics</returns>
    Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cleanup of expired items across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleaned up</returns>
    Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cleanup of expired items in a specific namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleaned up</returns>
    Task<int> CleanupExpiredItemsAsync(string cacheNamespace, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache statistics for a specific namespace.
/// </summary>
public class CacheNamespaceStats
{
    /// <summary>
    /// Cache namespace name.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Total number of items in the namespace.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Estimated total size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Number of expired items.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    /// Oldest item timestamp.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    /// Newest item timestamp.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }

    /// <summary>
    /// Average access count for items in the namespace.
    /// </summary>
    public double AverageAccessCount { get; set; }
}

/// <summary>
/// Overall cache statistics across all namespaces.
/// </summary>
public class CacheStats
{
    /// <summary>
    /// Total number of items across all namespaces.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Estimated total size in bytes across all namespaces.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Current quota limit in bytes.
    /// </summary>
    public long QuotaLimitBytes { get; set; }

    /// <summary>
    /// Percentage of quota used.
    /// </summary>
    public double QuotaUsagePercent { get; set; }

    /// <summary>
    /// Number of different namespaces.
    /// </summary>
    public int NamespaceCount { get; set; }

    /// <summary>
    /// Statistics for each namespace.
    /// </summary>
    public Dictionary<string, CacheNamespaceStats> NamespaceStats { get; set; } = new();

    /// <summary>
    /// Total number of expired items across all namespaces.
    /// </summary>
    public int TotalExpiredItemsCount { get; set; }

    /// <summary>
    /// Oldest item timestamp across all namespaces.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    /// Newest item timestamp across all namespaces.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }
}
