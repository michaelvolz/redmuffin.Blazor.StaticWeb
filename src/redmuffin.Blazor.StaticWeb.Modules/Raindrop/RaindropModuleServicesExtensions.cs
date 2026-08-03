using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Cache;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

public static class RaindropModuleServicesExtensions
{
    // Eager DI for tests and non-lazy hosts. WASM host must not call this at
    // cold start when the implementation assembly is lazy-loaded.
    public static IServiceCollection AddRaindropModule(this IServiceCollection services, bool useSyntheticData)
    {
        services.TryAddScoped<RaindropAPI>();
        services.TryAddScoped<DummyRaindropAPI>();
        services.TryAddScoped<IRaindropItemsCache, RaindropItemsCache>();
        services.TryAddScoped<IRaindropItemsStorage, RaindropItemsStorageAdapter>();
        services.TryAddScoped<IRaindropItemsFacade, RaindropItemsFacade>();

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

    // Strategy-selected facade after the implementation assembly is loaded.
    public static IRaindropItemsFacade CreateRaindropItemsFacade(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILocalStorageService localStorage,
        bool useSyntheticData)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(localStorage);

        var cache = new RaindropItemsCache(
            localStorage,
            loggerFactory.CreateLogger<RaindropItemsCache>());
        var storage = new RaindropItemsStorageAdapter(cache);

        DummyRaindropAPI? dummyApi = null;
        RaindropAPI? realApi = null;
        try
        {
            if (useSyntheticData)
            {
                dummyApi = new DummyRaindropAPI(
                    httpClientFactory,
                    loggerFactory.CreateLogger<DummyRaindropAPI>());
                var facade = new RaindropItemsFacade(dummyApi, storage);
                dummyApi = null; // ownership transferred to facade (SPA lifetime)
                return facade;
            }

            realApi = new RaindropAPI(
                httpClientFactory,
                loggerFactory.CreateLogger<RaindropAPI>());
            var realFacade = new RaindropItemsFacade(realApi, storage);
            realApi = null; // ownership transferred to facade (SPA lifetime)
            return realFacade;
        }
        finally
        {
            dummyApi?.Dispose();
            realApi?.Dispose();
        }
    }
}