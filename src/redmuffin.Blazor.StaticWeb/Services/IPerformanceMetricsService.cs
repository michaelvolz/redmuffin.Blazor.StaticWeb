using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service for collecting and processing web performance metrics
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    ///     Retrieves comprehensive performance metrics from the browser
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Performance metrics or null if unavailable</returns>
    Task<LoadSpeed.PageLoadMetrics?> GetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves WebAssembly-specific performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>WASM metrics or default values if unavailable</returns>
    Task<WasmMetrics> GetWasmMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if JavaScript performance APIs are available
    /// </summary>
    /// <returns>True if JavaScript APIs are available</returns>
    ValueTask<bool> IsJavaScriptAvailableAsync();

    /// <summary>
    ///     Gets legacy timing data for backward compatibility
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Array of timing values</returns>
    Task<double[]?> GetLegacyTimingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets fallback timing estimates when APIs are unavailable
    /// </summary>
    /// <returns>Estimated timing metrics</returns>
    Task<LoadSpeed.PageLoadMetrics> GetFallbackTimingAsync();
}