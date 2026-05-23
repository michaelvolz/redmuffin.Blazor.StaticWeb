using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;

/// <summary>
///     Interface for performance metrics service
/// </summary>
public interface IPerformanceMetricsService
{
    Task<PageLoadMetrics?> GetMetricsAsync(CancellationToken cancellationToken = default);

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
    Task<PageLoadMetrics> GetFallbackTimingAsync();
}