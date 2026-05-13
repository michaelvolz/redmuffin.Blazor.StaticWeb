using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class TimingMetricsCard
{
    [Parameter]
    public TimingMetrics? Timing { get; set; }

    private static string MetricColor(double value, double good, double poor) => value switch
    {
        _ when value <= good => PageLoadColors.LighthouseGood,
        _ when value <= poor => PageLoadColors.LighthouseWarning,
        _ => PageLoadColors.LighthousePoor
    };

    private static string TTFBColor(double value) => MetricColor(value, 800, 1800);
    private static string FCPColor(double value) => MetricColor(value, 1800, 3000);
    private static string LCPColor(double value) => MetricColor(value, 2500, 4000);
    private static string DOMColor(double value) => MetricColor(value, 1500, 3000);
    private static string LoadColor(double value) => MetricColor(value, 2500, 5000);
}
