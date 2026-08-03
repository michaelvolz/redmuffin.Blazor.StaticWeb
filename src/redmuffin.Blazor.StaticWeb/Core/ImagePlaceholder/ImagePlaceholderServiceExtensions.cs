using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder;

/// <summary>
///     Host DI registration for Core image validation and placeholder services.
///     Page-facing contracts are <see cref="IImageUrlResolver"/> and
///     <see cref="IImagePlaceholderService"/> in Common; collaborators stay Core-internal.
/// </summary>
public static class ImagePlaceholderServiceExtensions
{
    /// <summary>
    ///     Registers image URL resolution, placeholders, validation, and generation.
    /// </summary>
    public static IServiceCollection AddImagePlaceholderServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IImageValidator, ImageValidator>();
        services.AddScoped<IImagePlaceholderService, ImagePlaceholderService>();
        services.AddScoped<IImageUrlResolver, ImageUrlResolver>();
        services.AddScoped<PlaceholderGenerationService>();

        return services;
    }
}
