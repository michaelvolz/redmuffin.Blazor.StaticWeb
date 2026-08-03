using System.Reflection;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.Layout;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;
using redmuffin.Blazor.StaticWeb.Features.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb;

public partial class App
{
    // Fully-qualified so the host has no compile-time type root on the lazy impl assembly.
    private const string CreateHealthCheckServiceTypeName =
        "redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.AzureHealthCheckModuleServicesExtensions, AzureHealthCheck";

    private const string CreateHealthCheckServiceMethodName = "CreateHealthCheckService";

    private const string CreateRaindropItemsFacadeTypeName =
        "redmuffin.Blazor.StaticWeb.Modules.Raindrop.RaindropModuleServicesExtensions, Raindrop";

    private const string CreateRaindropItemsFacadeMethodName = "CreateRaindropItemsFacade";

    public ErrorBoundary ComponentErrorBoundary { get; set; } = null!;

    [Inject] private IWarmupService WarmupService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IPageAssemblyLoader PageAssemblyLoader { get; set; } = default!;
    [Inject] private AzureHealthCheckModuleGate AzureHealthCheckModuleGate { get; set; } = default!;
    [Inject] private AzureHealthCheckLoadOptions AzureHealthCheckLoadOptions { get; set; } = default!;
    [Inject] private RaindropModuleGate RaindropModuleGate { get; set; } = default!;
    [Inject] private RaindropLoadOptions RaindropLoadOptions { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

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

        await PageAssemblyLoader.EnsureLoadedAsync(pageKey).ConfigureAwait(false);

        if (pageKey.Equals(PageAssemblyCatalog.ApiHealthPageKey, StringComparison.OrdinalIgnoreCase))
            EnsureAzureHealthCheckServiceReady();

        if (pageKey.Equals(PageAssemblyCatalog.ArticlesPageKey, StringComparison.OrdinalIgnoreCase)
            || pageKey.Equals(PageAssemblyCatalog.VideosPageKey, StringComparison.OrdinalIgnoreCase))
            EnsureRaindropFacadeReady();
    }

    private void EnsureAzureHealthCheckServiceReady()
    {
        // Must run only after AzureHealthCheck.dll is loaded (no static type roots to the lazy DLL).
        if (AzureHealthCheckModuleGate.IsReady)
            return;

        var service = CreateHealthCheckServiceViaReflection(
            HttpClientFactory,
            LoggerFactory,
            AzureHealthCheckLoadOptions.UseSyntheticData);
        AzureHealthCheckModuleGate.SetService(service);
    }

    private void EnsureRaindropFacadeReady()
    {
        // Must run only after Raindrop.dll is loaded (no static type roots to the lazy DLL).
        if (RaindropModuleGate.IsReady)
            return;

        var facade = CreateRaindropItemsFacadeViaReflection(
            HttpClientFactory,
            LoggerFactory,
            LocalStorage,
            RaindropLoadOptions.UseSyntheticData);
        RaindropModuleGate.SetFacade(facade);
    }

    private static IHealthCheckService CreateHealthCheckServiceViaReflection(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        bool useSyntheticData)
    {
        var extensionsType = Type.GetType(CreateHealthCheckServiceTypeName, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Could not resolve type '{CreateHealthCheckServiceTypeName}' after loading AzureHealthCheck.dll.");

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

    private static IRaindropItemsFacade CreateRaindropItemsFacadeViaReflection(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILocalStorageService localStorage,
        bool useSyntheticData)
    {
        var extensionsType = Type.GetType(CreateRaindropItemsFacadeTypeName, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Could not resolve type '{CreateRaindropItemsFacadeTypeName}' after loading Raindrop.dll.");

        var method = extensionsType.GetMethod(
                CreateRaindropItemsFacadeMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types:
                [
                    typeof(IHttpClientFactory),
                    typeof(ILoggerFactory),
                    typeof(ILocalStorageService),
                    typeof(bool)
                ],
                modifiers: null)
            ?? throw new InvalidOperationException(
                $"Could not find {CreateRaindropItemsFacadeMethodName} on {extensionsType.FullName}.");

        var result = method.Invoke(null, [httpClientFactory, loggerFactory, localStorage, useSyntheticData]);
        return result as IRaindropItemsFacade
            ?? throw new InvalidOperationException(
                $"{CreateRaindropItemsFacadeMethodName} did not return IRaindropItemsFacade.");
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
