using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public static class ApiHealthModuleServicesExtensions
{
    public static IServiceCollection AddApiHealthModule(this IServiceCollection services, bool useSyntheticData)
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
}
