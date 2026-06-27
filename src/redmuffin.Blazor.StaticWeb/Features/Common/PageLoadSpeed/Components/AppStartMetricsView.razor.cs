using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Configuration;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

/// <summary>
///     Displays full application startup performance metrics including
///     page load timing and WebAssembly bootstrap metrics.
/// </summary>
public partial class AppStartMetricsView : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _disposed;
    private bool _isHidden;
    private bool _isCollapsed;
    private bool _isRefreshing;
    private ElementReference _speedElement;
    private PerformanceMetrics? _currentMetrics;

    private PerformanceCache _performanceCache = PerformanceCache.Create(0);

    [Inject] private IPerformanceMetricsService PerformanceService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<AppStartMetricsView> Logger { get; set; } = default!;

    private bool IsActive => !_disposed && !_cancellationTokenSource.IsCancellationRequested;

    private PerformanceCache PerformanceCache
    {
        get
        {
            if (_currentMetrics.HasValue)
            {
                var newCache = _currentMetrics.Value.GetPerformanceCache();
                if (Math.Abs(newCache.PrimaryMetric - _performanceCache.PrimaryMetric) > 0.1)
                    _performanceCache = newCache;
            }

            return _performanceCache;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !IsActive) return;
        if (!PageLoadSpeedConfig.ShouldDisplayComponent(Navigation.BaseUri)) return;

        try
        {
            if (!_currentMetrics.HasValue)
            {
                _currentMetrics = new PerformanceMetrics(
                    new TimingMetrics(0, 0, 0, 0, 0),
                    WasmMetrics.CreateDefault(),
                    new SizeMetrics(0, 0, 0, "Loading...", "Loading...", "Loading..."),
                    new CalculatedMetrics(0, 0, 0),
                    DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

                if (!IsActive) return;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }

            if (!IsActive) return;
            await UpdateMetricsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogInitializationFailed(Logger, ex);
            await SetFallbackMetricsAsync(ex).ConfigureAwait(false);
            await EnsureUIUpdatesAsync().ConfigureAwait(false);
        }
    }

    private async Task UpdateMetricsAsync()
    {
        if (!IsActive) return;

        try
        {
            await TryGetComprehensiveMetricsAsync().ConfigureAwait(false);
        }
        finally
        {
            await EnsureUIUpdatesAsync().ConfigureAwait(false);
        }
    }

    private async Task TryGetComprehensiveMetricsAsync()
    {
        if (!IsActive) return;

        try
        {
            var metrics = await PerformanceService.GetMetricsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            if (!IsActive) return;

            if (metrics != null)
            {
                _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                    metrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                return;
            }

            await TryGetLegacyMetricsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await SetFallbackMetricsAsync(ex).ConfigureAwait(false);
        }
    }

    private async Task TryGetLegacyMetricsAsync()
    {
        if (!IsActive) return;

        var legacyTimings = await PerformanceService.GetLegacyTimingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        if (!IsActive) return;

        if (legacyTimings is { Length: >= 2 })
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
            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                legacyMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            return;
        }

        var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
        if (!IsActive) return;

        _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
            fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private async Task SetFallbackMetricsAsync(Exception originalException)
    {
        if (!IsActive) return;

        try
        {
            var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
            if (!IsActive) return;

            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }
        catch (Exception fallbackException) when (fallbackException is not OperationCanceledException)
        {
            ApplyUnknownMetrics();
            LogFallbackMetricsFailed(Logger, originalException.Message, fallbackException);
        }
    }

    private void ApplyUnknownMetrics()
    {
        _currentMetrics = new PerformanceMetrics(
            new TimingMetrics(0, 0, 0, 0, 0),
            WasmMetrics.CreateDefault(),
            new SizeMetrics(0, 0, 0, "Unknown", "Unknown", "Unknown"),
            new CalculatedMetrics(0, 0, 0),
            DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private async Task EnsureUIUpdatesAsync()
    {
        if (!IsActive) return;

        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private void ToggleVisibility()
    {
        _isHidden = !_isHidden;
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " ")
            ToggleVisibility();
    }

    private void ToggleCollapsed()
    {
        _isCollapsed = !_isCollapsed;
    }

    private async Task RefreshMetricsAsync()
    {
        if (_isRefreshing || !IsActive) return;

        _isRefreshing = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            await UpdateMetricsAsync().ConfigureAwait(false);
        }
        finally
        {
            _isRefreshing = false;
        }
    }
}
