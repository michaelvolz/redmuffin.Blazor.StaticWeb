namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Storage usage statistics.
/// </summary>
public class StorageStats
{
    /// <summary>
    ///     Total number of items stored.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Estimated total size in bytes.
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
    ///     Number of items accessed recently.
    /// </summary>
    public int RecentlyAccessedCount { get; set; }

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
}