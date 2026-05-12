namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Overall cache statistics across all namespaces.
/// </summary>
public class CacheStats
{
    /// <summary>
    ///     Gets or sets total number of items across all namespaces.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Gets or sets estimated total size in bytes across all namespaces.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    ///     Gets or sets current quota limit in bytes.
    /// </summary>
    public long QuotaLimitBytes { get; set; }

    /// <summary>
    ///     Gets or sets percentage of quota used.
    /// </summary>
    public double QuotaUsagePercent { get; set; }

    /// <summary>
    ///     Gets or sets number of different namespaces.
    /// </summary>
    public int NamespaceCount { get; set; }

    /// <summary>
    ///     Gets or sets statistics for each namespace.
    /// </summary>
    public IDictionary<string, CacheNamespaceStats> NamespaceStats { get; set; } =
        new Dictionary<string, CacheNamespaceStats>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets total number of expired items across all namespaces.
    /// </summary>
    public int TotalExpiredItemsCount { get; set; }

    /// <summary>
    ///     Gets or sets oldest item timestamp across all namespaces.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets newest item timestamp across all namespaces.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }
}