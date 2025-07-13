namespace redmuffin.Blazor.StaticWeb.Models.PageLoadSpeed;

/// <summary>
///     Comprehensive performance metrics container
/// </summary>
public readonly record struct PerformanceMetrics(
    TimingMetrics Timing,
    SizeMetrics Size,
    CalculatedMetrics Calculated,
    string FormattedTimestamp)
{
    public PerformanceCache GetPerformanceCache()
    {
        return PerformanceCache.Create(Timing.PrimaryMetric);
    }

    public static PerformanceMetrics FromPageLoadMetrics(
        Features.Shared.Components.PageLoadSpeed.PageLoadMetrics metrics,
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