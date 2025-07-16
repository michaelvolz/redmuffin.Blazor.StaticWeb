using System.Runtime.InteropServices;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Immutable timing metrics record
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct TimingMetrics(
    double TimeToFirstByte,
    double DomContentLoaded,
    double LoadComplete,
    double FirstContentfulPaint,
    double LargestContentfulPaint)
{
    public double PrimaryMetric => LargestContentfulPaint > 0 ? LargestContentfulPaint : LoadComplete;
}