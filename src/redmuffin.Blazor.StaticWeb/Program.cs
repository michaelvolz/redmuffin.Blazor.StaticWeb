using Blazored.LocalStorage;
using Mediator;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Services;
using redmuffin.Blazor.StaticWeb.Features.DebugPage.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

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

// Register cache services
builder.Services.AddScoped<IRaindropItemsCache, RaindropItemsCache>();
builder.Services.AddScoped<LocalStorageDebugService>();

// Register delay provider for production (real delays for UX)
builder.Services.AddScoped<IDelayProvider, ProductionDelayProvider>();

// Register Mediator with source-generated handlers and pipeline behaviors
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddModulePipelineBehaviors();

// Synthetic data only on pure client host (localhost:5233), same policy as Raindrop.
// SWA local (localhost:4280) and production use the real HTTP implementation.
var useSyntheticApiHealth = builder.HostEnvironment.BaseAddress.Contains(
    "localhost:5233",
    StringComparison.OrdinalIgnoreCase);
builder.Services.AddApiHealthModule(useSyntheticApiHealth);

// Register Raindrop services with factory pattern
builder.Services.AddScoped<IRaindropAPIFactory, RaindropAPIFactory>();
builder.Services.AddScoped<DummyRaindropAPI>();
builder.Services.AddScoped<RaindropAPI>();
builder.Services.AddScoped<IRaindropAPI>(serviceProvider =>
{
    var factory = serviceProvider.GetRequiredService<IRaindropAPIFactory>();
    return factory.CreateRaindropAPI();
});

await builder.Build().RunAsync().ConfigureAwait(false);