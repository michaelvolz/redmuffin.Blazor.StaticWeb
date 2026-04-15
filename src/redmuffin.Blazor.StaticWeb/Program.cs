using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Cache.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;
using redmuffin.Blazor.StaticWeb.Services;

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

builder.Services.AddScoped<ISimpleImageValidationService, SimpleImageValidationService>();
builder.Services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();

// Register image placeholder services
builder.Services.AddScoped<IImagePlaceholderService, ImagePlaceholderService>();
builder.Services.AddScoped<IImageValidationCacheService, ImageValidationCacheService>();
builder.Services.AddScoped<PlaceholderGenerationService>();

// Register cache services
builder.Services.AddScoped<IRaindropItemsCache, RaindropItemsCache>();
builder.Services.AddScoped<LocalStorageDebugService>();

// Register delay provider for production (real delays for UX)
builder.Services.AddScoped<IDelayProvider, ProductionDelayProvider>();

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