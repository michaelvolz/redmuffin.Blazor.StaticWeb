namespace redmuffin.Blazor.StaticWeb.Common.Enums;

/// <summary>
/// Represents the source of an image extracted from web content.
/// </summary>
public enum ImageSource
{
    /// <summary>
    /// Image extracted from Open Graph meta tags (og:image).
    /// </summary>
    OpenGraph,

    /// <summary>
    /// Image extracted from Twitter Card meta tags (twitter:image).
    /// </summary>
    Twitter,

    /// <summary>
    /// Image extracted from Apple Touch Icon meta tags (apple-touch-icon).
    /// </summary>
    Apple,

    /// <summary>
    /// Image extracted from favicon link tags.
    /// </summary>
    Favicon,

    /// <summary>
    /// Image extracted from generic meta tags or other sources.
    /// </summary>
    Generic,

    /// <summary>
    /// No image found or extraction failed.
    /// </summary>
    None
}
