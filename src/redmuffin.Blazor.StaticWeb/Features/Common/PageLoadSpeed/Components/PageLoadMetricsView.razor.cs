using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Configuration;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

/// <summary>
///     Displays page-load performance metrics using Navigation Timing API data.
///     Shows timing, data transfer, calculated breakdowns, and performance rating.
/// </summary>
public partial class PageLoadMetricsView : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isHidden;
    private bool _isCollapsed;
    private bool _isRefreshing;
    private ElementReference _speedElement;
    private PerformanceMetrics? _currentMetrics;

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
                if (Math.Abs(newCache.PrimaryMetric - _performanceCache.PrimaryMetric) > 0.1)
                    _performanceCache = newCache;
            }

            return _performanceCache;
        }
    }

    private static PageLoadMetrics CreateLegacyMetrics(double[] legacyTimings) =>
        new()
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

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var shouldDisplay = PageLoadSpeedConfig.ShouldDisplayComponent(Navigation.BaseUri);

        if (!firstRender || !shouldDisplay) return;

        try
        {
            await InitializeWithEmptyMetricsAsync().ConfigureAwait(false);
            await UpdateMetricsAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during disposal
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PageLoadMetricsView initialization failed: {ex.Message}");
        }
    }

    private async Task InitializeWithEmptyMetricsAsync()
    {
        if (_currentMetrics.HasValue) return;

        _currentMetrics = new PerformanceMetrics(
            new TimingMetrics(0, 0, 0, 0, 0),
            WasmMetrics.CreateDefault(),
            new SizeMetrics(0, 0, 0, "Loading...", "Loading...", "Loading..."),
            new CalculatedMetrics(0, 0, 0),
            DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
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
            await EnsureUIUpdatesAsync().ConfigureAwait(false);
        }
    }

    private async Task TryGetComprehensiveMetricsAsync()
    {
        try
        {
            var metrics = await PerformanceService.GetMetricsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            if (metrics != null)
            {
                _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                    metrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                return;
            }

            await TryGetLegacyMetricsAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
        catch (Exception)
        {
            await SetFallbackMetricsAsync().ConfigureAwait(false);
        }
    }

    private async Task TryGetLegacyMetricsAsync()
    {
        var legacyTimings = await PerformanceService.GetLegacyTimingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        if (legacyTimings is { Length: >= 2 })
        {
            var legacyMetrics = CreateLegacyMetrics(legacyTimings);
            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                legacyMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            return;
        }

        var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
        _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
            fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
    }

    private async Task SetFallbackMetricsAsync()
    {
        try
        {
            var fallbackMetrics = await PerformanceService.GetFallbackTimingAsync().ConfigureAwait(false);
            _currentMetrics = PerformanceMetrics.FromPageLoadMetrics(
                fallbackMetrics, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }
        catch (Exception)
        {
            _currentMetrics = new PerformanceMetrics(
                new TimingMetrics(0, 0, 0, 0, 0),
                WasmMetrics.CreateDefault(),
                new SizeMetrics(0, 0, 0, "Unknown", "Unknown", "Unknown"),
                new CalculatedMetrics(0, 0, 0),
                DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }
    }

    private async Task EnsureUIUpdatesAsync()
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;

        try
        {
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PageLoadMetricsView UI update failed: {ex.Message}");
        }
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
        }
    }
}
