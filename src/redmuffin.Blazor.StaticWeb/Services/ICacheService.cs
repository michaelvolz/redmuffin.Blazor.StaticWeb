namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for cache management with namespace separation for different data types.
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     Stores an item in the specified cache namespace.
    /// </summary>
    /// <typeparam name="T">Type of the item to store</typeparam>
    /// <param name="cacheNamespace">Cache namespace to separate different types of data</param>
    /// <param name="key">Storage key</param>
    /// <param name="value">Value to store</param>
    /// <param name="expirationMinutes">Expiration time in minutes (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetItemAsync<T>(string cacheNamespace, string key, T value, int? expirationMinutes = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves an item from the specified cache namespace.
    /// </summary>
    /// <typeparam name="T">Type of the item to retrieve</typeparam>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored item or default value if not found</returns>
    Task<T?> GetItemAsync<T>(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes an item from the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveItemAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an item exists in the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the item exists, false otherwise</returns>
    Task<bool> ContainsKeyAsync(string cacheNamespace, string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all keys in the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of keys in the namespace</returns>
    Task<IEnumerable<string>> GetKeysAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all items from the specified cache namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets cache statistics for the specified namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache statistics for the namespace</returns>
    Task<CacheNamespaceStats> GetNamespaceStatsAsync(string cacheNamespace, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets overall cache statistics across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall cache statistics</returns>
    Task<CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs cleanup of expired items across all namespaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleaned up</returns>
    Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs cleanup of expired items in a specific namespace.
    /// </summary>
    /// <param name="cacheNamespace">Cache namespace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleaned up</returns>
    Task<int> CleanupExpiredItemsAsync(string cacheNamespace, CancellationToken cancellationToken = default);
}