namespace redmuffin.Blazor.StaticWeb.Features.ArticlesPage.Models;

/// <summary>
///     Defines the main processing phases for article image processing.
/// </summary>
public enum ProcessingPhase
{
    /// <summary>
    ///     No processing has been initiated.
    /// </summary>
    None,

    /// <summary>
    ///     Article is queued for processing.
    /// </summary>
    Queued,

    /// <summary>
    ///     Article is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    ///     Processing completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     Processing failed.
    /// </summary>
    Failed
}