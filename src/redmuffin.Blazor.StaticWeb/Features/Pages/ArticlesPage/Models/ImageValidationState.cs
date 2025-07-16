namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Models;

/// <summary>
///     Defines the image validation states for image URL verification.
/// </summary>
public enum ImageValidationState
{
    /// <summary>
    ///     Image validation not started.
    /// </summary>
    None,

    /// <summary>
    ///     Validating image URL accessibility.
    /// </summary>
    Validating,

    /// <summary>
    ///     Image validation completed successfully.
    /// </summary>
    Validated,

    /// <summary>
    ///     Image validation failed.
    /// </summary>
    Failed
}