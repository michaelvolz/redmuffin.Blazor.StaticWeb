using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Common.PageLoadSpeed;

[Category("Feature:Common")]
public sealed partial class PerformanceMetricsServiceTests
{
    [Test]
    public async Task GetMetrics_And_GetWasmMetrics_Keep_Stable_Blazor_Init_And_Send_MarkEnd_At_Most_Once()
    {
        // Arrange
        var jsRuntime = new PageLoadMetricsJsRuntime();
        var service = new PerformanceMetricsService(jsRuntime);

        try
        {
            // Act
            var pageMetricsRead = await service.GetMetricsAsync().ConfigureAwait(false);
            var wasmMetricsRead = await service.GetWasmMetricsAsync().ConfigureAwait(false);
            var secondPageMetricsRead = await service.GetMetricsAsync().ConfigureAwait(false);

            // Assert
            using (Assert.Multiple())
            {
                await Assert.That(pageMetricsRead).IsNotNull();
                await Assert.That(secondPageMetricsRead).IsNotNull();
                await Assert.That(wasmMetricsRead.BlazorInitTime).IsGreaterThan(0);
                await Assert
                    .That(pageMetricsRead?.BlazorInitTime)
                    .IsEqualTo(wasmMetricsRead.BlazorInitTime);
                await Assert
                    .That(secondPageMetricsRead?.BlazorInitTime)
                    .IsEqualTo(wasmMetricsRead.BlazorInitTime);

                var markEndExpressions = jsRuntime.EvalExpressions.Count(expression =>
                    expression.Contains("markEnd()", StringComparison.Ordinal)
                );
                await Assert.That(markEndExpressions).IsLessThanOrEqualTo(1);
            }
        }
        finally
        {
            await service.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class PageLoadMetricsJsRuntime : IJSRuntime
    {
        private bool _markEndSent;

        public List<string> EvalExpressions { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identifier == "eval")
            {
                var expression = args?.FirstOrDefault()?.ToString() ?? string.Empty;
                EvalExpressions.Add(expression);

                if (
                    expression.Contains(
                        "typeof window.getPageLoadMetrics === 'function'",
                        StringComparison.Ordinal
                    )
                )
                {
                    return ValueTask.FromResult((TValue)(object)true);
                }

                if (
                    expression.Contains(
                        "typeof window.getWasmMetrics === 'function'",
                        StringComparison.Ordinal
                    )
                )
                {
                    return ValueTask.FromResult((TValue)(object)true);
                }

                if (expression.Contains("markEnd()", StringComparison.Ordinal))
                {
                    _markEndSent = true;
                    return ValueTask.FromResult(default(TValue)!);
                }

                if (expression.Contains("performance.now", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)1234d);
                }
            }

            if (identifier == "getPageLoadMetrics")
            {
                return ValueTask.FromResult(
                    (TValue)
                        (object)
                            new PageLoadMetrics
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
                                ResourceLoadTime = 4,
                            }
                );
            }

            if (identifier == "getWasmMetrics")
            {
                var endOffset = _markEndSent ? 100d : 0d;

                return ValueTask.FromResult(
                    (TValue)
                        (object)
                            new WasmMetrics(
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
                            )
                );
            }

            throw new InvalidOperationException($"Unexpected JS call: {identifier}");
        }
    }
}
