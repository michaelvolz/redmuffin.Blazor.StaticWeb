namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache usage recommendations.
/// </summary>
public class CacheRecommendations
{
    /// <summary>
    ///     Recommended cache size adjustments.
    /// </summary>
    public IList<string> SizeRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Recommended expiration policy changes.
    /// </summary>
    public IList<string> ExpirationRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Recommended performance optimizations.
    /// </summary>
    public IList<string> PerformanceRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Recommended maintenance actions.
    /// </summary>
    public IList<string> MaintenanceRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Overall cache health score (0-100).
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    ///     Recommendations generated timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}