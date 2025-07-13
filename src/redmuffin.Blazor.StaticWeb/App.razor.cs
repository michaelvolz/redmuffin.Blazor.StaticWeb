using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Core.Layout;
using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb;

public partial class App
{
    public ErrorBoundary ComponentErrorBoundary { get; set; } = null!;

    [Inject] private IWarmupService WarmupService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Fire-and-forget warm-up of Azure Functions
        _ = Task.Run(async () => await WarmupService.WarmupAsync().ConfigureAwait(false));

        await base.OnInitializedAsync().ConfigureAwait(false);
    }

    private static Task HandleNavigationAsync(NavigationContext args)
    {
        // Ensure the layout type is preserved for trimming
        _ = typeof(MainLayout);
        return Task.CompletedTask;
    }
}
