namespace redmuffin.Blazor.StaticWeb.Models;

/// <summary>
/// Cached performance calculations to avoid repeated computations
/// </summary>
public readonly record struct PerformanceCache(
    PerformanceRating Rating,
    string RatingText,
    string CssClass,
    string Icon,
    int Score,
    double PrimaryMetric)
{
    public static PerformanceCache Create(double primaryMetric) => primaryMetric switch
    {
        <= 1000 => new(PerformanceRating.Excellent, "EXCELLENT", "excellent", "🚀", 95, primaryMetric),
        <= 2500 => new(PerformanceRating.Good, "GOOD", "good", "✅", 75, primaryMetric),
        <= 4000 => new(PerformanceRating.Fair, "FAIR", "fair", "⚠️", 50, primaryMetric),
        _ => new(PerformanceRating.Poor, "POOR", "poor", "🐌", 25, primaryMetric),
    };
}
