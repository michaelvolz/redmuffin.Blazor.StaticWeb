using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.Layout;
using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb;

public partial class App
{
    public ErrorBoundary ComponentErrorBoundary { get; set; } = null!;

    [Inject] private IWarmupService WarmupService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private static Task HandleNavigationAsync(NavigationContext args)
    {
        // Ensure the layout type is preserved for trimming
        _ = typeof(MainLayout);
        return Task.CompletedTask;
    }

    protected override Task OnInitializedAsync()
    {
        // Mark the boundary between WASM runtime ready and Blazor initialization
        _ = JSRuntime.InvokeVoidAsync("eval", "window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markBlazorStart()").AsTask();

        // Fire-and-forget warm-up of Azure Functions
        _ = Task.Run(() => WarmupService.WarmupAsync());

        return base.OnInitializedAsync();
    }
}