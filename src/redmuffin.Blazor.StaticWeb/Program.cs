using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using redmuffin.Blazor.StaticWeb.Common.Services;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("DefaultClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IBrowserStorageService, BrowserStorageService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IWarmupService, WarmupService>();
builder.Services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();
builder.Services.AddScoped<IImageValidationService, ImageValidationService>();
builder.Services.AddScoped<IOpenGraphImagesService, OpenGraphImagesService>();
builder.Services.AddScoped<ISimpleImageValidationService, SimpleImageValidationService>();

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