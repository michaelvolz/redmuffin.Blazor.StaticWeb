using Blazored.LocalStorage;
using Mediator;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;
using redmuffin.Blazor.StaticWeb.Features.DebugPage.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Set minimum log level to Warning in production to reduce browser console noise
// Users who need verbose logs can use browser DevTools directly
builder.Logging.SetMinimumLevel(
    builder.HostEnvironment.IsProduction() ? LogLevel.Warning : LogLevel.Information);

builder.Services.AddHttpClient(string.Empty, client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IBrowserStorageService, BrowserStorageService>();
builder.Services.AddScoped<IWarmupService, WarmupService>();

builder.Services.AddScoped<IImageValidator, ImageValidator>();
builder.Services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();

// Register image placeholder services
builder.Services.AddScoped<IImagePlaceholderService, ImagePlaceholderService>();
builder.Services.AddScoped<IImageUrlResolver, ImageUrlResolver>();
builder.Services.AddScoped<PlaceholderGenerationService>();

builder.Services.AddScoped<LocalStorageDebugService>();

// Register delay provider for production (real delays for UX)
builder.Services.AddScoped<IDelayProvider, ProductionDelayProvider>();

// Register Mediator with source-generated handlers and pipeline behaviors
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddModulePipelineBehaviors();

// Synthetic data only on pure client host (localhost:5233).
// SWA local (localhost:4280) and production use the real HTTP implementation.
var useSynthetic = builder.HostEnvironment.BaseAddress.Contains(
    "localhost:5233",
    StringComparison.OrdinalIgnoreCase);

// AzureHealthCheck / Raindrop implementation assemblies are lazy-loaded (see App + catalog).
// Do not call AddAzureHealthCheckModule / AddRaindropModule here — that would force impl DLLs at boot.
// PageAssemblyLoader: navigate loads + Home Articles/Videos prefetch.
builder.Services.AddScoped<LazyAssemblyLoader>();
builder.Services.AddScoped<IPageAssemblyLoader, PageAssemblyLoader>();
builder.Services.AddSingleton(new AzureHealthCheckLoadOptions(useSynthetic));
builder.Services.AddSingleton<AzureHealthCheckModuleGate>();
builder.Services.AddScoped<IHealthCheckService>(static sp =>
    sp.GetRequiredService<AzureHealthCheckModuleGate>().GetRequiredService());

builder.Services.AddSingleton(new RaindropLoadOptions(useSynthetic));
builder.Services.AddSingleton<RaindropModuleGate>();
builder.Services.AddScoped<IRaindropItemsFacade>(static sp =>
    sp.GetRequiredService<RaindropModuleGate>().GetRequiredFacade());

await builder.Build().RunAsync().ConfigureAwait(false);