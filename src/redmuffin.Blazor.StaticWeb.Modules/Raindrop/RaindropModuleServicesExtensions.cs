using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

public static class RaindropModuleServicesExtensions
{
    public static IServiceCollection AddRaindropModule(this IServiceCollection services, bool useSyntheticData)
    {
        services.TryAddScoped<RaindropAPI>();
        services.TryAddScoped<DummyRaindropAPI>();

        if (useSyntheticData)
        {
            services.AddScoped<IRaindropAPI>(static sp => sp.GetRequiredService<DummyRaindropAPI>());
        }
        else
        {
            services.AddScoped<IRaindropAPI>(static sp => sp.GetRequiredService<RaindropAPI>());
        }

        return services;
    }
}
