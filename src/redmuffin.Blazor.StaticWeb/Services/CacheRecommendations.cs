namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache usage recommendations.
/// </summary>
public class CacheRecommendations
{
    /// <summary>
    ///     Gets or sets recommended cache size adjustments.
    /// </summary>
    public IList<string> SizeRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets recommended expiration policy changes.
    /// </summary>
    public IList<string> ExpirationRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets recommended performance optimizations.
    /// </summary>
    public IList<string> PerformanceRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets recommended maintenance actions.
    /// </summary>
    public IList<string> MaintenanceRecommendations { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets overall cache health score (0-100).
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    ///     Gets or sets recommendations generated timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}