using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Common.PageLoadSpeed;

[Category("Feature:Common")]
public sealed partial class PerformanceMetricsServiceTests
{
    [Test]
    public async Task GetMetricsAsync_Should_Preserve_Blazor_Init_After_Initial_Finalization()
    {
        // Arrange
        var jsRuntime = new PageLoadMetricsJsRuntime();
        var service = new PerformanceMetricsService(jsRuntime);

        try
        {
            // Act
            var firstMetrics = await service.GetMetricsAsync().ConfigureAwait(false);
            var secondMetrics = await service.GetMetricsAsync().ConfigureAwait(false);

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(firstMetrics).IsNotNull();
                await Assert.That(secondMetrics).IsNotNull();
                await Assert.That(firstMetrics?.BlazorInitTime).IsEqualTo(100);
                await Assert.That(secondMetrics?.BlazorInitTime).IsEqualTo(100);
                await Assert.That(jsRuntime.MarkEndCallCount).IsEqualTo(1);
            }
        }

        finally
        {
            await service.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task GetWasmMetricsAsync_Should_Return_Stable_Blazor_Init_On_Repeated_Reads()
    {
        // Arrange
        var jsRuntime = new PageLoadMetricsJsRuntime();
        var service = new PerformanceMetricsService(jsRuntime);

        try
        {
            // Act
            var firstMetrics = await service.GetWasmMetricsAsync().ConfigureAwait(false);
            var secondMetrics = await service.GetWasmMetricsAsync().ConfigureAwait(false);

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(firstMetrics.BlazorInitTime).IsEqualTo(100);
                await Assert.That(secondMetrics.BlazorInitTime).IsEqualTo(100);
                await Assert.That(jsRuntime.MarkEndCallCount).IsEqualTo(1);
            }
        }

        finally
        {
            await service.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class PageLoadMetricsJsRuntime : IJSRuntime
    {
        public int MarkEndCallCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identifier == "eval")
            {
                var expression = args?.FirstOrDefault()?.ToString() ?? string.Empty;

                if (expression.Contains("typeof window.getPageLoadMetrics === 'function'", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)true);
                }

                if (expression.Contains("typeof window.getWasmMetrics === 'function'", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)true);
                }

                if (expression.Contains("window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markEnd()", StringComparison.Ordinal))
                {
                    MarkEndCallCount++;
                    return ValueTask.FromResult(default(TValue)!);
                }

                if (expression.Contains("performance.now", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)1234d);
                }
            }

            if (identifier == "getPageLoadMetrics")
            {
                return ValueTask.FromResult((TValue)(object)new PageLoadMetrics
                {
                    TimeToFirstByte = 12,
                    DomContentLoaded = 34,
                    LoadComplete = 56,
                    FirstContentfulPaint = 78,
                    LargestContentfulPaint = 90,
                    TransferSize = 1,
                    EncodedSize = 1,
                    DecodedSize = 1,
                    TransferSizeFormatted = "1 B",
                    EncodedSizeFormatted = "1 B",
                    DecodedSizeFormatted = "1 B",
                    ServerResponseTime = 2,
                    DomProcessingTime = 3,
                    ResourceLoadTime = 4
                });
            }

            if (identifier == "getWasmMetrics")
            {
                var endOffset = MarkEndCallCount > 0 ? 100d : 0d;

                return ValueTask.FromResult((TValue)(object)new WasmMetrics
                (
                    WasmDownloadTime: 11,
                    WasmDownloadSize: 22,
                    WasmDownloadSizeFormatted: "22 B",
                    AssemblyCount: 3,
                    AssemblyTotalSize: 44,
                    AssemblyTotalSizeFormatted: "44 B",
                    RuntimeStartupTime: 55,
                    MemoryUsed: 66,
                    MemoryTotal: 77,
                    MemoryFormatted: "66 MB / 77 MB",
                    BlazorInitTime: endOffset
                ));
            }

            throw new InvalidOperationException($"Unexpected JS call: {identifier}");
        }
    }
}
