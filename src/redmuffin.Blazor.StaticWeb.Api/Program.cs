using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Common;

var host = new HostBuilder()
	.ConfigureLogging( configureLogging =>
	{
		configureLogging.ClearProviders();
		configureLogging.AddConsole();
		configureLogging.AddDebug();
		configureLogging.AddFilter("Microsoft", LogLevel.Warning);
		configureLogging.AddFilter("System", LogLevel.Warning);
	})
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
		services.AddSingleton<Settings>();
		services.AddSingleton<ILogger>(provider =>
		{
			var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("AzureFunctionLogger");
			return new PrefixedLogger(logger, "AzureFunction");
		});
	})
	.Build();

var logger = host.Services.GetRequiredService<ILogger>();

// Test log message
logger.LogInformation("This is a test log message.");

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
