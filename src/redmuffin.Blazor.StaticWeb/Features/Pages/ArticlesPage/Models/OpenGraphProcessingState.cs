namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Models;

/// <summary>
///     Defines the OpenGraph processing states for server-side image retrieval.
/// </summary>
public enum OpenGraphProcessingState
{
    /// <summary>
    ///     OpenGraph processing not started.
    /// </summary>
    None,

    /// <summary>
    ///     Fetching HTML content from the article URL.
    /// </summary>
    FetchingHtml,

    /// <summary>
    ///     Parsing OpenGraph metadata from HTML.
    /// </summary>
    ParsingMetadata,

    /// <summary>
    ///     Extracting image URLs from metadata.
    /// </summary>
    ExtractingImages,

    /// <summary>
    ///     OpenGraph processing completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     OpenGraph processing failed.
    /// </summary>
    Failed
}