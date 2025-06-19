using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using redmuffin.Blazor.StaticWeb.Api.Core;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
		services.AddSingleton<Settings>();
	})
	.Build();

var settings = host.Services.GetRequiredService<Settings>();

// Output settings to console for debugging
Console.WriteLine($"RainDropClientId: {settings.RainDropClientId}");
Console.WriteLine($"RainDropClientSecret: {settings.RainDropClientSecret}");
Console.WriteLine($"RainDropTestToken: {settings.RainDropTestToken}");

// Validate Settings
if (string.IsNullOrWhiteSpace(settings.RainDropClientId) ||
	string.IsNullOrWhiteSpace(settings.RainDropClientSecret) ||
	string.IsNullOrWhiteSpace(settings.RainDropTestToken))
	throw new InvalidOperationException("One or more settings are not configured. Please check local.settings.json or application settings.");

await host.RunAsync().ConfigureAwait(false);
