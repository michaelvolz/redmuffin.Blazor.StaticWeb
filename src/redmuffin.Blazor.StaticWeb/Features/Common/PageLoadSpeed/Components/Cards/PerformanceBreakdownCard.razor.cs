using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class PerformanceBreakdownCard
{
    [Parameter]
    public CalculatedMetrics? Calculated { get; set; }

    private bool HasData => Calculated is { } c
        && (c.ServerResponseTime > 0 || c.DomProcessingTime > 0 || c.ResourceLoadTime > 0);
}
