using Mediator;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.PipelineBehaviors;

namespace redmuffin.Blazor.StaticWeb.Common;

public static class MediatorServiceExtensions
{
    public static IServiceCollection AddModulePipelineBehaviors(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
