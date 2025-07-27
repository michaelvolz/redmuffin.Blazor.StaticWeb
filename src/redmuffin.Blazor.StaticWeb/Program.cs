using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using redmuffin.Blazor.StaticWeb.Common.Services;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<object>(client =>
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