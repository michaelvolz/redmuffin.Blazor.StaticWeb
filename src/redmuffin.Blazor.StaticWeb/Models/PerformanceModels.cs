namespace redmuffin.Blazor.StaticWeb.Models;

/// <summary>
/// Performance rating categories based on Core Web Vitals
/// </summary>
public enum PerformanceRating
{
    Excellent,
    Good,
    Fair,
    Poor
}

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
        _ => new(PerformanceRating.Poor, "POOR", "poor", "🐌", 25, primaryMetric)
    };
}

/// <summary>
/// Immutable timing metrics record
/// </summary>
public readonly record struct TimingMetrics(
    double TimeToFirstByte,
    double DomContentLoaded,
    double LoadComplete,
    double FirstContentfulPaint,
    double LargestContentfulPaint)
{
    public double PrimaryMetric => LargestContentfulPaint > 0 ? LargestContentfulPaint : LoadComplete;
}

/// <summary>
/// Immutable size metrics record
/// </summary>
public readonly record struct SizeMetrics(
    double TransferSize,
    double EncodedSize,
    double DecodedSize,
    string TransferSizeFormatted,
    string EncodedSizeFormatted,
    string DecodedSizeFormatted)
{
    public double CompressionRatio => DecodedSize > 0 && EncodedSize > 0
        ? Math.Round(((DecodedSize - EncodedSize) / DecodedSize) * 100, 1)
        : 0;
}

/// <summary>
/// Immutable calculated metrics record
/// </summary>
public readonly record struct CalculatedMetrics(
    double ServerResponseTime,
    double DomProcessingTime,
    double ResourceLoadTime);

/// <summary>
/// Comprehensive performance metrics container
/// </summary>
public readonly record struct PerformanceMetrics(
    TimingMetrics Timing,
    SizeMetrics Size,
    CalculatedMetrics Calculated,
    string FormattedTimestamp)
{
    public PerformanceCache GetPerformanceCache() => PerformanceCache.Create(Timing.PrimaryMetric);
    
    public static PerformanceMetrics FromPageLoadMetrics(
        redmuffin.Blazor.StaticWeb.Features.Shared.Components.PageLoadSpeed.PageLoadMetrics metrics,
        string timestamp)
    {
        var timing = new TimingMetrics(
            metrics.TimeToFirstByte,
            metrics.DomContentLoaded,
            metrics.LoadComplete,
            metrics.FirstContentfulPaint,
            metrics.LargestContentfulPaint);

        var size = new SizeMetrics(
            metrics.TransferSize,
            metrics.EncodedSize,
            metrics.DecodedSize,
            metrics.TransferSizeFormatted,
            metrics.EncodedSizeFormatted,
            metrics.DecodedSizeFormatted);

        var calculated = new CalculatedMetrics(
            metrics.ServerResponseTime,
            metrics.DomProcessingTime,
            metrics.ResourceLoadTime);

        return new PerformanceMetrics(timing, size, calculated, timestamp);
    }
}
