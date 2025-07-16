namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache statistics for a specific namespace.
/// </summary>
public class CacheNamespaceStats
{
    /// <summary>
    ///     Cache namespace name.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Total number of items in the namespace.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Estimated total size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    ///     Number of expired items.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    ///     Oldest item timestamp.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    ///     Newest item timestamp.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }

    /// <summary>
    ///     Average access count for items in the namespace.
    /// </summary>
    public double AverageAccessCount { get; set; }
}