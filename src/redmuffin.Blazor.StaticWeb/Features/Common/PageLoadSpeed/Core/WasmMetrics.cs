namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Immutable WebAssembly metrics record containing WASM-specific performance data
/// </summary>
public readonly record struct WasmMetrics(
    double WasmDownloadTime,
    double WasmDownloadSize,
    string WasmDownloadSizeFormatted,
    int AssemblyCount,
    double AssemblyTotalSize,
    string AssemblyTotalSizeFormatted,
    double RuntimeStartupTime,
    double MemoryUsed,
    double MemoryTotal,
    string MemoryFormatted,
    double BlazorInitTime)
{
    /// <summary>
    ///     Creates a WasmMetrics instance with default/N/A values for unavailable metrics
    /// </summary>
    /// <returns></returns>
    public static WasmMetrics CreateDefault()
    {
        return new WasmMetrics(
            WasmDownloadTime: 0,
            WasmDownloadSize: 0,
            WasmDownloadSizeFormatted: "N/A",
            AssemblyCount: 0,
            AssemblyTotalSize: 0,
            AssemblyTotalSizeFormatted: "N/A",
            RuntimeStartupTime: 0,
            MemoryUsed: 0,
            MemoryTotal: 0,
            MemoryFormatted: "N/A",
            BlazorInitTime: 0);
    }
}
