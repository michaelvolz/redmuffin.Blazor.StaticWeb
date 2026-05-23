using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Components;

public partial class WasmBootstrapCard
{
    [Parameter]
    public WasmMetrics? Wasm { get; set; }
}
