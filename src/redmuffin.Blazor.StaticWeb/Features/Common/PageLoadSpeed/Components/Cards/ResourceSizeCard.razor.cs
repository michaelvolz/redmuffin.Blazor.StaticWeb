using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class ResourceSizeCard
{
    [Parameter]
    public SizeMetrics? Size { get; set; }

    private static double DataSizeProgress(double sizeBytes)
    {
        const double maxSize = 1024 * 1024;
        return Math.Min(100, sizeBytes / maxSize * 100);
    }
}
