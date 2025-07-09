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
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IWarmupService, WarmupService>();
builder.Services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();

await builder.Build().RunAsync().ConfigureAwait(false);