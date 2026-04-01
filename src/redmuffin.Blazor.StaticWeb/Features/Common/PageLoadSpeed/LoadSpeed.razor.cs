using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Configuration;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed;

/// <summary>
///     Code-behind for LoadSpeed component that displays performance metrics and analytics.
///     Implements sophisticated performance monitoring with comprehensive timing and size metrics.
/// </summary>
public partial class LoadSpeed : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isHidden;
    private bool _isCollapsed;
    private bool _isRefreshing;
    private ElementReference _speedElement;
    private PerformanceMetrics? _currentMetrics;

    // Cached performance data
    private PerformanceCache _performanceCache = PerformanceCache.Create(0);

    [Inject] private IPerformanceMetricsService PerformanceService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private PerformanceCache PerformanceCache
    {
        get
        {
            if (_currentMetrics.HasValue)
            {
                var newCache = _currentMetrics.Value.GetPerformanceCache();
                if (Math.Abs(newCache.PrimaryMetric - _performanceCache.PrimaryMetric) > 0.1) _performanceCache = newCache;
            }

            return _performanceCache;
        }
    }

    private static double GetProgressWidth(double value, double maxValue)
    {
        if (value <= 0 || maxValue <= 0) return 0;
        return Math.Min(100, value / maxValue * 100);
    }

    private static double GetDataSizeProgress(double sizeBytes)
    {
        // Progress based on typical web page size (1MB = 100%)
        const double maxSize = 1024 * 1024; // 1MB
        return Math.Min(100, sizeBytes / maxSize * 100);
    }

    private static string GetTimingColor(double timing)
    {
        return timing switch
        {
            <= 1000 => "#00ff41",
            <= 2500 => "#ffd700",
            <= 4000 => "#ff8c42",
            _ => "#ff4757"
        };
    }

    private static string GetWasmColor(double value, double excellent, double good, double poor)
    {
        return value switch
        {
            _ when value <= excellent => "#00ff41",
            _ when value <= good => "#ffd700",
            _ when value <= poor => "#ff8c42",
            _ => "#ff4757"
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var shouldDisplay = PageLoadSpeedConfig.ShouldDisplayComponent(Navigation.BaseUri);

        if (firstRender && shouldDisplay)
            try
            {
                // Initialize with empty metrics first to ensure UI shows something
                if (!_currentMetrics.HasValue)
                {
                    _currentMetrics = new PerformanceMetrics(
                        new TimingMetrics(0, 0, 0, 0, 0),
                        WasmMetrics.CreateDefault(),
                        new SizeMetrics(0, 0, 0, "Loading...", "Loading...", "Loading..."),
                        new CalculatedMetrics(0, 0, 0),
                        DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                }

                await Task.Delay(PageLoadSpeedConfig.AutoLoadDelayMs, _cancellationTokenSource.Token).ConfigureAwait(false);
                await UpdateMetricsAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal
            }
            catch (Exception ex)
            {
                // If initialization fails, component will still show UI with manual refresh option
                Debug.WriteLine($"Performance metrics initialization failed: {ex.Message}");
            }
    }

    private async Task UpdateMetricsAsync()
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;

        try
        {
            await TryGetComprehensiveMetricsAsync().ConfigureAwait(false);
        }
        finally
        {
            // Ensure UI updates after any metrics update
            if (!_cancellationTokenSource.IsCancellationRequested)
                try
                {
                    await InvokeAsync(StateHasChanged).ConfigureAwait(false);
                }
                catch
                {
                    // If InvokeAsync fails, component might be disposed
                }
        }
    }

    private async Task TryGetComprehensiveMetricsAsync()
    {
        try
        {
            // Try to get comprehensive metrics first
            var metrics = await PerformanceService.GetMetricsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            if (metrics != null)
            {
                _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(metrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                return;
            }

            await TryGetLegacyMetricsAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
        catch (Exception ex)
        {
            // Use fallback on any error
            await SetFallbackMetricsAsync(ex).ConfigureAwait(false);
        }
    }

    private async Task TryGetLegacyMetricsAsync()
    {
        // Try legacy timing
        var legacyTimings = await PerformanceService.GetLegacyTimingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        if (legacyTimings != null && legacyTimings.Length >= 2)
        {
            var legacyMetrics = new PageLoadMetrics
            {
                TimeToFirstByte = Math.Round(legacyTimings[1] * 0.3, 1),
                DomContentLoaded = Math.Round(legacyTimings[1], 1),
                LoadComplete = Math.Round(legacyTimings[0], 1),
                FirstContentfulPaint = 0,
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
            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(legacyMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            return;
        }

        // Use fallback timing
        var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
        _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private async Task SetFallbackMetricsAsync(Exception originalException)
    {
        try
        {
            var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }
        catch (Exception fallbackException)
        {
            // Final fallback - create empty metrics
            _currentMetrics = new PerformanceMetrics(
                new TimingMetrics(0, 0, 0, 0, 0),
                WasmMetrics.CreateDefault(),
                new SizeMetrics(0, 0, 0, "Unknown", "Unknown", "Unknown"),
                new CalculatedMetrics(0, 0, 0),
                DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

            Debug.WriteLine($"Fallback metrics failed: Original={originalException.Message}, Fallback={fallbackException.Message}");
        }
    }

    private void ToggleVisibility()
    {
        _isHidden = !_isHidden;
    }

    private void ToggleCollapsed()
    {
        _isCollapsed = !_isCollapsed;
    }

    private async Task RefreshMetricsAsync()
    {
        if (_isRefreshing) return;

        _isRefreshing = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            await UpdateMetricsAsync().ConfigureAwait(false);
        }
        finally
        {
            _isRefreshing = false;
            // UpdateMetricsAsync already calls StateHasChanged in finally block
        }
    }

    private bool HasBreakdownMetrics()
    {
        return (_currentMetrics?.Calculated.ServerResponseTime ?? 0) > 0 ||
               (_currentMetrics?.Calculated.DomProcessingTime ?? 0) > 0 ||
               (_currentMetrics?.Calculated.ResourceLoadTime ?? 0) > 0;
    }

    // Data model for JavaScript interop
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
}