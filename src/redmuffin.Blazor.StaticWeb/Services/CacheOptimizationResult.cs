namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache optimization result.
/// </summary>
public class CacheOptimizationResult
{
    /// <summary>
    ///     Gets or sets a value indicating whether whether optimization was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    ///     Gets or sets number of expired items removed.
    /// </summary>
    public int ExpiredItemsRemoved { get; set; }

    /// <summary>
    ///     Gets or sets number of items evicted for optimization.
    /// </summary>
    public int ItemsEvicted { get; set; }

    /// <summary>
    ///     Gets or sets storage space freed in bytes.
    /// </summary>
    public long StorageFreedBytes { get; set; }

    /// <summary>
    ///     Gets or sets time taken for optimization in milliseconds.
    /// </summary>
    public long OptimizationTimeMs { get; set; }

    /// <summary>
    ///     Gets or sets storage utilization before optimization.
    /// </summary>
    public double StorageUtilizationBefore { get; set; }

    /// <summary>
    ///     Gets or sets storage utilization after optimization.
    /// </summary>
    public double StorageUtilizationAfter { get; set; }

    /// <summary>
    ///     Gets or sets optimization actions performed.
    /// </summary>
    public IList<string> ActionsPerformed { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets optimization timestamp.
    /// </summary>
    public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
}