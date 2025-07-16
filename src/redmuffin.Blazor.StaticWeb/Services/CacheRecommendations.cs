namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache usage recommendations.
/// </summary>
public class CacheRecommendations
{
    /// <summary>
    ///     Recommended cache size adjustments.
    /// </summary>
    public List<string> SizeRecommendations { get; set; } = new();

    /// <summary>
    ///     Recommended expiration policy changes.
    /// </summary>
    public List<string> ExpirationRecommendations { get; set; } = new();

    /// <summary>
    ///     Recommended performance optimizations.
    /// </summary>
    public List<string> PerformanceRecommendations { get; set; } = new();

    /// <summary>
    ///     Recommended maintenance actions.
    /// </summary>
    public List<string> MaintenanceRecommendations { get; set; } = new();

    /// <summary>
    ///     Overall cache health score (0-100).
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    ///     Recommendations generated timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}