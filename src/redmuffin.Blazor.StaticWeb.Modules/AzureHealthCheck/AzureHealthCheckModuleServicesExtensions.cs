using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck;

public static class AzureHealthCheckModuleServicesExtensions
{
    // Eager DI for tests and non-lazy hosts. WASM host must not call this at
    // cold start when the implementation assembly is lazy-loaded.
    public static IServiceCollection AddAzureHealthCheckModule(
        this IServiceCollection services,
        bool useSyntheticData)
    {
        services.TryAddScoped<HealthCheckService>();
        services.TryAddScoped<SyntheticHealthCheckService>();

        if (useSyntheticData)
        {
            services.AddScoped<IHealthCheckService>(static sp => sp.GetRequiredService<SyntheticHealthCheckService>());
        }
        else
        {
            services.AddScoped<IHealthCheckService>(static sp => sp.GetRequiredService<HealthCheckService>());
        }

        return services;
    }

    // Strategy-selected service after the implementation assembly is loaded.
    public static IHealthCheckService CreateHealthCheckService(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        bool useSyntheticData)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (useSyntheticData)
            return new SyntheticHealthCheckService();

        return new HealthCheckService(
            httpClientFactory,
            loggerFactory.CreateLogger<HealthCheckService>());
    }
}
