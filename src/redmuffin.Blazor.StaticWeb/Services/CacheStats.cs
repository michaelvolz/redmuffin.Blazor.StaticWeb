namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Overall cache statistics across all namespaces.
/// </summary>
public class CacheStats
{
    /// <summary>
    ///     Total number of items across all namespaces.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Estimated total size in bytes across all namespaces.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    ///     Current quota limit in bytes.
    /// </summary>
    public long QuotaLimitBytes { get; set; }

    /// <summary>
    ///     Percentage of quota used.
    /// </summary>
    public double QuotaUsagePercent { get; set; }

    /// <summary>
    ///     Number of different namespaces.
    /// </summary>
    public int NamespaceCount { get; set; }

    /// <summary>
    ///     Statistics for each namespace.
    /// </summary>
    public Dictionary<string, CacheNamespaceStats> NamespaceStats { get; set; } = new();

    /// <summary>
    ///     Total number of expired items across all namespaces.
    /// </summary>
    public int TotalExpiredItemsCount { get; set; }

    /// <summary>
    ///     Oldest item timestamp across all namespaces.
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    ///     Newest item timestamp across all namespaces.
    /// </summary>
    public DateTime? NewestItemTimestamp { get; set; }
}