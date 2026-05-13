namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Cached performance calculations to avoid repeated computations
/// </summary>
public readonly record struct PerformanceCache(
    PerformanceRating Rating,
    string RatingText,
    string CssClass,
    string Icon,
    int Score,
    double PrimaryMetric)
{
    public static PerformanceCache Create(double primaryMetric)
    {
        // Thresholds based on Google LCP standard: Good <2.5s, Poor >4.0s.
        // PrimaryMetric is LCP when available, LoadComplete otherwise.
        return primaryMetric switch
        {
            <= 1500 => new PerformanceCache(PerformanceRating.Excellent, "EXCELLENT", "excellent", "🚀", 95, primaryMetric),
            <= 2500 => new PerformanceCache(PerformanceRating.Good, "GOOD", "good", "✅", 75, primaryMetric),
            <= 4000 => new PerformanceCache(PerformanceRating.Fair, "FAIR", "fair", "⚠️", 50, primaryMetric),
            _ => new PerformanceCache(PerformanceRating.Poor, "POOR", "poor", "🐌", 25, primaryMetric)
        };
    }
}