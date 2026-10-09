using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder;

/// <summary>
///     Host DI registration for Core placeholder and image resolution services.
///     Page-facing contracts are <see cref="IImageUrlResolver"/> and
///     <see cref="IImagePlaceholderService"/> in Common; collaborators stay Core-internal.
/// </summary>
public static class ImagePlaceholderServiceExtensions
{
    /// <summary>
    ///     Registers image URL resolution, placeholders, and generation.
    /// </summary>
    public static IServiceCollection AddImagePlaceholderServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IImagePlaceholderService, ImagePlaceholderService>();
        services.AddScoped<IImageUrlResolver, ImageUrlResolver>();
        services.AddScoped<PlaceholderGenerationService>();

        return services;
    }
}
