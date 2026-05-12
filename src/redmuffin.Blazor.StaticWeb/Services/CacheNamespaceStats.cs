namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache statistics for a specific namespace.
/// </summary>
public class CacheNamespaceStats
{
    /// <summary>
    ///     Gets or sets cache namespace name.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets total number of items in the namespace.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Gets or sets estimated total size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    ///     Gets or sets number of expired items.
    /// </summary>
    public int ExpiredItemsCount { get; set; }

    /// <summary>
    ///     Gets or sets oldest item timestamp.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets newest item timestamp.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets average access count for items in the namespace.
    /// </summary>
    public double AverageAccessCount { get; set; }
}