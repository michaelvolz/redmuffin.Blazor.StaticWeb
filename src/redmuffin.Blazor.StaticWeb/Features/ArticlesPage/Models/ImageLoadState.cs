namespace redmuffin.Blazor.StaticWeb.Features.ArticlesPage.Models;

/// <summary>
///     Defines the image loading states for client-side image display.
/// </summary>
public enum ImageLoadState
{
    /// <summary>
    ///     Image loading state is unknown.
    /// </summary>
    None,

    /// <summary>
    ///     Image is currently loading.
    /// </summary>
    Loading,

    /// <summary>
    ///     Image loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    ///     Image failed to load.
    /// </summary>
    Failed
}