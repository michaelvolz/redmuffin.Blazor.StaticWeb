namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Data model for JavaScript interop. Contains all page load metrics
///     (timing, WASM, size, calculated) from browser APIs.
/// </summary>
public class PageLoadMetrics
{
    // Timing metrics
    public double TimeToFirstByte { get; set; }
    public double DomContentLoaded { get; set; }
    public double LoadComplete { get; set; }
    public double FirstContentfulPaint { get; set; }
    public double LargestContentfulPaint { get; set; }

    // WASM metrics
    public double WasmDownloadTime { get; set; }
    public double WasmDownloadSize { get; set; }
    public string WasmDownloadSizeFormatted { get; set; } = string.Empty;
    public int AssemblyCount { get; set; }
    public double AssemblyTotalSize { get; set; }
    public string AssemblyTotalSizeFormatted { get; set; } = string.Empty;
    public double RuntimeStartupTime { get; set; }
    public double MemoryUsed { get; set; }
    public double MemoryTotal { get; set; }
    public string MemoryFormatted { get; set; } = string.Empty;
    public double BlazorInitTime { get; set; }

    // Size metrics
    public double TransferSize { get; set; }
    public double EncodedSize { get; set; }
    public double DecodedSize { get; set; }
    public string TransferSizeFormatted { get; set; } = string.Empty;
    public string EncodedSizeFormatted { get; set; } = string.Empty;
    public string DecodedSizeFormatted { get; set; } = string.Empty;

    // Calculated metrics
    public double ServerResponseTime { get; set; }
    public double DomProcessingTime { get; set; }
    public double ResourceLoadTime { get; set; }
}
