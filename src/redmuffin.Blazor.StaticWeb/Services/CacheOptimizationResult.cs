namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache optimization result.
/// </summary>
public class CacheOptimizationResult
{
    /// <summary>
    ///     Whether optimization was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    ///     Number of expired items removed.
    /// </summary>
    public int ExpiredItemsRemoved { get; set; }

    /// <summary>
    ///     Number of items evicted for optimization.
    /// </summary>
    public int ItemsEvicted { get; set; }

    /// <summary>
    ///     Storage space freed in bytes.
    /// </summary>
    public long StorageFreedBytes { get; set; }

    /// <summary>
    ///     Time taken for optimization in milliseconds.
    /// </summary>
    public long OptimizationTimeMs { get; set; }

    /// <summary>
    ///     Storage utilization before optimization.
    /// </summary>
    public double StorageUtilizationBefore { get; set; }

    /// <summary>
    ///     Storage utilization after optimization.
    /// </summary>
    public double StorageUtilizationAfter { get; set; }

    /// <summary>
    ///     Optimization actions performed.
    /// </summary>
    public IList<string> ActionsPerformed { get; set; } = new List<string>();

    /// <summary>
    ///     Optimization timestamp.
    /// </summary>
    public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;
}