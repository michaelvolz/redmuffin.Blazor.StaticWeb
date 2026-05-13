using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Configuration;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Service implementation for collecting web performance metrics via JavaScript interop
/// </summary>
public class PerformanceMetricsService(IJSRuntime jsRuntime) : IPerformanceMetricsService, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;
    private bool _wasmInitFinalized;

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _disposed = true;

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<PageLoadMetrics?> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(PageLoadSpeedConfig.JsInteropTimeoutSeconds));

            await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                var functionExists = await jsRuntime.InvokeAsync<bool>("eval", cts.Token, "typeof window.getPageLoadMetrics === 'function'")
                    .ConfigureAwait(false);

                if (functionExists)
                {
                    if (!_wasmInitFinalized)
                    {
                        // Finalize the startup boundary once; later reads must not move it.
                        await jsRuntime.InvokeVoidAsync("eval", cts.Token, "window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markEnd()").ConfigureAwait(false);
                        _wasmInitFinalized = true;
                    }

                    var metrics = await jsRuntime.InvokeAsync<PageLoadMetrics>("getPageLoadMetrics", cts.Token).ConfigureAwait(false);

                    // Fetch WASM metrics directly (semaphore already held by GetMetricsAsync)
                    var wasmMetrics = await jsRuntime.InvokeAsync<WasmMetrics>("getWasmMetrics", cts.Token).ConfigureAwait(false);
                    metrics.WasmDownloadTime = wasmMetrics.WasmDownloadTime;
                    metrics.WasmDownloadSize = wasmMetrics.WasmDownloadSize;
                    metrics.WasmDownloadSizeFormatted = wasmMetrics.WasmDownloadSizeFormatted;
                    metrics.AssemblyCount = wasmMetrics.AssemblyCount;
                    metrics.AssemblyTotalSize = wasmMetrics.AssemblyTotalSize;
                    metrics.AssemblyTotalSizeFormatted = wasmMetrics.AssemblyTotalSizeFormatted;
                    metrics.RuntimeStartupTime = wasmMetrics.RuntimeStartupTime;
                    metrics.MemoryUsed = wasmMetrics.MemoryUsed;
                    metrics.MemoryTotal = wasmMetrics.MemoryTotal;
                    metrics.MemoryFormatted = wasmMetrics.MemoryFormatted;
                    metrics.BlazorInitTime = wasmMetrics.BlazorInitTime;

                    return metrics;
                }

                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<WasmMetrics> GetWasmMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return WasmMetrics.CreateDefault();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(PageLoadSpeedConfig.JsInteropTimeoutSeconds));

            await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                var functionExists = await jsRuntime.InvokeAsync<bool>("eval", cts.Token, "typeof window.getWasmMetrics === 'function'")
                    .ConfigureAwait(false);

                if (functionExists)
                {
                    if (!_wasmInitFinalized)
                    {
                        // Finalize the startup boundary once; later reads must not move it.
                        await jsRuntime.InvokeVoidAsync("eval", cts.Token, "window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markEnd()").ConfigureAwait(false);
                        _wasmInitFinalized = true;
                    }

                    var metrics = await jsRuntime.InvokeAsync<WasmMetrics>("getWasmMetrics", cts.Token).ConfigureAwait(false);
                    return metrics;
                }

                return WasmMetrics.CreateDefault();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return WasmMetrics.CreateDefault();
        }
        catch (Exception)
        {
            return WasmMetrics.CreateDefault();
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsJavaScriptAvailableAsync()
    {
        if (_disposed) return false;

        try
        {
            return await jsRuntime.InvokeAsync<bool>("eval", "typeof window.getPageLoadMetrics === 'function'").ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<double[]?> GetLegacyTimingAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(PageLoadSpeedConfig.JsInteropTimeoutSeconds));

            await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                var functionExists = await jsRuntime.InvokeAsync<bool>("eval", cts.Token, "typeof window.getPageLoadTimes === 'function'")
                    .ConfigureAwait(false);

                if (functionExists)
                {
                    var timings = await jsRuntime.InvokeAsync<double[]>("getPageLoadTimes", cts.Token).ConfigureAwait(false);
                    return timings?.Length >= 2 ? timings : null;
                }

                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PageLoadMetrics> GetFallbackTimingAsync()
    {
        try
        {
            var now = await jsRuntime.InvokeAsync<double>("performance.now").ConfigureAwait(false);
            return new PageLoadMetrics
            {
                TimeToFirstByte = Math.Round(now * 0.3, 1),
                DomContentLoaded = Math.Round(now * 0.8, 1),
                LoadComplete = Math.Round(now, 1),
                FirstContentfulPaint = Math.Round(now * 0.6, 1),
                LargestContentfulPaint = 0,
                TransferSize = 0,
                EncodedSize = 0,
                DecodedSize = 0,
                TransferSizeFormatted = "Unknown",
                EncodedSizeFormatted = "Unknown",
                DecodedSizeFormatted = "Unknown",
                ServerResponseTime = 0,
                DomProcessingTime = 0,
                ResourceLoadTime = 0
            };
        }
        catch (Exception)
        {
            var estimatedTime = DateTime.Now.Millisecond + 100;
            return new PageLoadMetrics
            {
                TimeToFirstByte = estimatedTime * 0.3,
                DomContentLoaded = estimatedTime * 0.8,
                LoadComplete = estimatedTime,
                FirstContentfulPaint = estimatedTime * 0.6,
                LargestContentfulPaint = 0,
                TransferSize = 0,
                EncodedSize = 0,
                DecodedSize = 0,
                TransferSizeFormatted = "Unknown",
                EncodedSizeFormatted = "Unknown",
                DecodedSizeFormatted = "Unknown",
                ServerResponseTime = 0,
                DomProcessingTime = 0,
                ResourceLoadTime = 0
            };
        }
    }
}
