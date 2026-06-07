using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public static class ApiHealthModuleServicesExtensions
{
    public static IServiceCollection AddApiHealthModule(this IServiceCollection services)
    {
        services.TryAddScoped<HealthCheckService>();
        services.TryAddScoped<SyntheticHealthCheckService>();
        return services;
    }
}
