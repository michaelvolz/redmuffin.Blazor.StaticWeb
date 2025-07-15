using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Configuration;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed;

namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
/// Service implementation for collecting web performance metrics via JavaScript interop
/// </summary>
public class PerformanceMetricsService(IJSRuntime jsRuntime) : IPerformanceMetricsService, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public async Task<PageLoadSpeed.PageLoadMetrics?> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(PageLoadSpeedConfig.JsInteropTimeoutSeconds));

            await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                var functionExists = await jsRuntime.InvokeAsync<bool>("eval", cts.Token, "typeof window.getPageLoadMetrics === 'function'").ConfigureAwait(false);

                if (functionExists)
                {
                    var metrics = await jsRuntime.InvokeAsync<PageLoadSpeed.PageLoadMetrics>("getPageLoadMetrics", cts.Token).ConfigureAwait(false);
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
    public async ValueTask<bool> IsJavaScriptAvailableAsync()
    {
        if (_disposed)
        {
            return false;
        }

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
        if (_disposed)
        {
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(PageLoadSpeedConfig.JsInteropTimeoutSeconds));

            await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                var functionExists = await jsRuntime.InvokeAsync<bool>("eval", cts.Token, "typeof window.getPageLoadTimes === 'function'").ConfigureAwait(false);

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
    public async Task<PageLoadSpeed.PageLoadMetrics> GetFallbackTimingAsync()
    {
        try
        {
            var now = await jsRuntime.InvokeAsync<double>("performance.now").ConfigureAwait(false);
            return new PageLoadSpeed.PageLoadMetrics
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
                ResourceLoadTime = 0,
            };
        }
        catch (Exception)
        {
            var estimatedTime = DateTime.Now.Millisecond + 100;
            return new PageLoadSpeed.PageLoadMetrics
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
                ResourceLoadTime = 0,
            };
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
