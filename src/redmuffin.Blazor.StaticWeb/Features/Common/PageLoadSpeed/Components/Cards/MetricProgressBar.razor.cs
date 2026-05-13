using Microsoft.AspNetCore.Components;
namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class MetricProgressBar
{
    [Parameter]
    public double Value { get; set; }

    [Parameter]
    public double MaxValue { get; set; }

    [Parameter]
    public string Color { get; set; } = "#00bfff";

    private double ProgressWidth => Value <= 0 || MaxValue <= 0 ? 0 : Math.Min(100, Value / MaxValue * 100);
}
