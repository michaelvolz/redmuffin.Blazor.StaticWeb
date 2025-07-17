namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for enhanced browser storage management with LRU eviction and quota management.
/// </summary>
public interface IBrowserStorageService
{
    /// <summary>
    ///     Stores an item with automatic LRU eviction if quota is exceeded.
    /// </summary>
    /// <typeparam name="T">Type of the item to store</typeparam>
    /// <param name="key">Storage key</param>
    /// <param name="value">Value to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves an item from storage and updates its LRU position.
    /// </summary>
    /// <typeparam name="T">Type of the item to retrieve</typeparam>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored item or default value if not found</returns>
    Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes an item from storage.
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveItemAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an item exists in storage.
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the item exists, false otherwise</returns>
    Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all keys currently stored.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all storage keys</returns>
    Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all items from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets current storage usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage usage statistics</returns>
    Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs LRU eviction to free up space.
    /// </summary>
    /// <param name="targetSizeBytes">Target size to achieve after eviction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items evicted</returns>
    Task<int> EvictLeastRecentlyUsedAsync(long targetSizeBytes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Estimates the storage size used by a specific key.
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Estimated size in bytes</returns>
    Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs cleanup of expired items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleaned up</returns>
    Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the storage quota limit.
    /// </summary>
    /// <param name="quotaBytes">Quota limit in bytes</param>
    void SetQuotaLimit(long quotaBytes);

    /// <summary>
    ///     Gets the current quota limit.
    /// </summary>
    /// <returns>Quota limit in bytes</returns>
    long GetQuotaLimit();

    /// <summary>
    ///     Completely clears all localStorage data including both cached items and the storage index.
    ///     This is a more thorough clear operation than ClearAsync which only clears managed storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items cleared</returns>
    Task<int> ClearAllStorageAsync(CancellationToken cancellationToken = default);
}