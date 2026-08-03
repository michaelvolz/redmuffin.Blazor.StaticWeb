using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.Layout;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.ApiHealth;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb;

public partial class App
{
    // Fully-qualified so the host has no compile-time type root on the lazy impl assembly.
    private const string CreateHealthCheckServiceTypeName =
        "redmuffin.Blazor.StaticWeb.Modules.ApiHealth.ApiHealthModuleServicesExtensions, ApiHealth";

    private const string CreateHealthCheckServiceMethodName = "CreateHealthCheckService";

    public ErrorBoundary ComponentErrorBoundary { get; set; } = null!;

    [Inject] private IWarmupService WarmupService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IPageAssemblyLoader PageAssemblyLoader { get; set; } = default!;
    [Inject] private ApiHealthModuleGate ApiHealthModuleGate { get; set; } = default!;
    [Inject] private ApiHealthLoadOptions ApiHealthLoadOptions { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;

    /// <summary>
    ///     Gets the lazy page assemblies for <c>Router.AdditionalAssemblies</c>.
    /// </summary>
    private IReadOnlyList<Assembly> LazyLoadedAssemblies => PageAssemblyLoader.LoadedAssemblies;

    private async Task HandleNavigationAsync(NavigationContext args)
    {
        // Ensure the layout type is preserved for trimming
        _ = typeof(MainLayout);

        if (!PageAssemblyCatalog.TryGetPageKeyFromPath(args.Path, out var pageKey))
            return;

        // Catalog empty for a page key → EnsureLoadedAsync no-ops (dormant Articles/Videos).
        await PageAssemblyLoader.EnsureLoadedAsync(pageKey).ConfigureAwait(false);

        if (pageKey.Equals(PageAssemblyCatalog.ApiHealthPageKey, StringComparison.OrdinalIgnoreCase))
            EnsureApiHealthServiceReady();
    }

    private void EnsureApiHealthServiceReady()
    {
        // Must run only after ApiHealth.dll is loaded (no static type roots to the lazy DLL).
        if (ApiHealthModuleGate.IsReady)
            return;

        var service = CreateHealthCheckServiceViaReflection(
            HttpClientFactory,
            LoggerFactory,
            ApiHealthLoadOptions.UseSyntheticData);
        ApiHealthModuleGate.SetService(service);
    }

    private static IHealthCheckService CreateHealthCheckServiceViaReflection(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        bool useSyntheticData)
    {
        var extensionsType = Type.GetType(CreateHealthCheckServiceTypeName, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Could not resolve type '{CreateHealthCheckServiceTypeName}' after loading ApiHealth.dll.");

        var method = extensionsType.GetMethod(
                CreateHealthCheckServiceMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(IHttpClientFactory), typeof(ILoggerFactory), typeof(bool)],
                modifiers: null)
            ?? throw new InvalidOperationException(
                $"Could not find {CreateHealthCheckServiceMethodName} on {extensionsType.FullName}.");

        var result = method.Invoke(null, [httpClientFactory, loggerFactory, useSyntheticData]);
        return result as IHealthCheckService
            ?? throw new InvalidOperationException(
                $"{CreateHealthCheckServiceMethodName} did not return IHealthCheckService.");
    }

    protected override Task OnInitializedAsync()
    {
        // Mark the boundary between WASM runtime ready and Blazor initialization
        _ = JSRuntime.InvokeVoidAsync("eval", "window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markBlazorStart()").AsTask();

        // Best-effort warm-up of Azure Functions; failure does not block startup
        _ = WarmupService.TryWarmupAsync();

        return base.OnInitializedAsync();
    }
}
