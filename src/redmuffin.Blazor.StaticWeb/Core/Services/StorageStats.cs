namespace redmuffin.Blazor.StaticWeb.Core.Services;

/// <summary>
///     Storage usage statistics.
/// </summary>
public class StorageStats
{
    /// <summary>
    ///     Gets or sets total number of items stored.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    ///     Gets or sets estimated total size in bytes.
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
    ///     Gets or sets number of items accessed recently.
    /// </summary>
    public int RecentlyAccessedCount { get; set; }

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
}