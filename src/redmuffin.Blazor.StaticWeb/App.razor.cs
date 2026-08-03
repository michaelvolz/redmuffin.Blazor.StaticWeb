using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
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

    private readonly List<Assembly> _lazyLoadedAssemblies = [];

    public ErrorBoundary ComponentErrorBoundary { get; set; } = null!;

    [Inject] private IWarmupService WarmupService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private LazyAssemblyLoader AssemblyLoader { get; set; } = default!;
    [Inject] private ApiHealthModuleGate ApiHealthModuleGate { get; set; } = default!;
    [Inject] private ApiHealthLoadOptions ApiHealthLoadOptions { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;

    private async Task HandleNavigationAsync(NavigationContext args)
    {
        // Ensure the layout type is preserved for trimming
        _ = typeof(MainLayout);

        if (IsApiHealthRoute(args.Path))
            await EnsureApiHealthModuleLoadedAsync().ConfigureAwait(false);
    }

    private static bool IsApiHealthRoute(string path)
    {
        var trimmed = path.Trim('/');
        return trimmed.Equals("api-health", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureApiHealthModuleLoadedAsync()
    {
        if (ApiHealthModuleGate.IsReady)
            return;

        // Must load before any type from the ApiHealth implementation assembly is used.
        // No static type refs to ApiHealth — those force a FileNotFoundException before this runs.
        var assemblies = await AssemblyLoader.LoadAssembliesAsync(["ApiHealth.dll"]).ConfigureAwait(false);
        foreach (var assembly in assemblies)
        {
            if (!_lazyLoadedAssemblies.Contains(assembly))
                _lazyLoadedAssemblies.Add(assembly);
        }

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
