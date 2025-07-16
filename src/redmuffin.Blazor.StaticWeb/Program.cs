using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using redmuffin.Blazor.StaticWeb;
using redmuffin.Blazor.StaticWeb.Core.Services;
using redmuffin.Blazor.StaticWeb.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("DefaultHttpClient", client => { client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });

// Configure HttpClient for external requests with HTTPS preference
builder.Services.AddHttpClient("ExternalHttpClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "redmuffin-blazor-staticweb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "image/*, */*");
});

builder.Services.AddHttpClient();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IBrowserStorageService, BrowserStorageService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IWarmupService, WarmupService>();
builder.Services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();
builder.Services.AddScoped<IImageValidationService, ImageValidationService>();
builder.Services.AddScoped<IOpenGraphImagesService, OpenGraphImagesService>();

await builder.Build().RunAsync().ConfigureAwait(false);