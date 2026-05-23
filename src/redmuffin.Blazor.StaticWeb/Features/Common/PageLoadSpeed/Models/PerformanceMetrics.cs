namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;

/// <summary>
///     Comprehensive performance metrics container
/// </summary>
public readonly record struct PerformanceMetrics(
    TimingMetrics Timing,
    WasmMetrics Wasm,
    SizeMetrics Size,
    CalculatedMetrics Calculated,
    string FormattedTimestamp)
{
    public PerformanceCache GetPerformanceCache()
    {
        return PerformanceCache.Create(Timing.PrimaryMetric);
    }

    public static PerformanceMetrics FromPageLoadMetrics(
        PageLoadMetrics metrics,
        string timestamp)
    {
        var timing = new TimingMetrics(
            metrics.TimeToFirstByte,
            metrics.DomContentLoaded,
            metrics.LoadComplete,
            metrics.FirstContentfulPaint,
            metrics.LargestContentfulPaint);

        var wasm = new WasmMetrics(
            metrics.WasmDownloadTime,
            metrics.WasmDownloadSize,
            metrics.WasmDownloadSizeFormatted,
            metrics.AssemblyCount,
            metrics.AssemblyTotalSize,
            metrics.AssemblyTotalSizeFormatted,
            metrics.RuntimeStartupTime,
            metrics.MemoryUsed,
            metrics.MemoryTotal,
            metrics.MemoryFormatted,
            metrics.BlazorInitTime);

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

        return new PerformanceMetrics(timing, wasm, size, calculated, timestamp);
    }
}