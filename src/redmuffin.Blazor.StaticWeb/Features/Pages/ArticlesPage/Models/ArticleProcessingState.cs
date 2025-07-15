using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Models;

/// <summary>
/// Represents the comprehensive processing state of an article's image processing workflow.
/// </summary>
public sealed class ArticleProcessingState
{
    /// <summary>
    /// Gets or sets the article link (used as unique identifier).
    /// </summary>
    public string ArticleLink { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current processing phase of the article.
    /// </summary>
    public ProcessingPhase ProcessingPhase { get; set; } = ProcessingPhase.None;

    /// <summary>
    /// Gets or sets the current image loading state.
    /// </summary>
    public ImageLoadState ImageLoadState { get; set; } = ImageLoadState.None;

    /// <summary>
    /// Gets or sets the OpenGraph image processing state.
    /// </summary>
    public OpenGraphProcessingState OpenGraphState { get; set; } = OpenGraphProcessingState.None;

    /// <summary>
    /// Gets or sets the image validation state.
    /// </summary>
    public ImageValidationState ValidationState { get; set; } = ImageValidationState.None;

    /// <summary>
    /// Gets or sets the cached enhanced image data.
    /// </summary>
    public CachedImageData? EnhancedImage { get; set; }

    /// <summary>
    /// Gets or sets the fallback reason when image processing fails.
    /// </summary>
    public string? FallbackReason { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing started.
    /// </summary>
    public DateTime ProcessingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing completed.
    /// </summary>
    public DateTime? ProcessingCompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the processing error message, if any.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts for this article.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets the total processing duration if completed.
    /// </summary>
    public TimeSpan? ProcessingDuration => ProcessingCompletedAt.HasValue 
        ? ProcessingCompletedAt.Value - ProcessingStartedAt 
        : null;

    /// <summary>
    /// Gets whether the article processing is in a final state (completed or failed).
    /// </summary>
    public bool IsInFinalState => ProcessingPhase is ProcessingPhase.Completed or ProcessingPhase.Failed;

    /// <summary>
    /// Gets whether the article can be retried.
    /// </summary>
    public bool CanRetry => ProcessingPhase == ProcessingPhase.Failed && RetryCount < MaxRetryAttempts;

    /// <summary>
    /// Gets whether the article is currently being processed.
    /// </summary>
    public bool IsProcessing => ProcessingPhase == ProcessingPhase.Processing;

    /// <summary>
    /// Gets whether the article processing was successful.
    /// </summary>
    public bool IsSuccessful => ProcessingPhase == ProcessingPhase.Completed && 
                               EnhancedImage?.IsValidated == true && 
                               !string.IsNullOrEmpty(EnhancedImage.ImageUrl);

    /// <summary>
    /// Gets whether a fallback placeholder should be shown.
    /// </summary>
    public bool ShouldShowFallback => ImageLoadState == ImageLoadState.Failed || 
                                     (ProcessingPhase == ProcessingPhase.Failed && EnhancedImage == null);

    /// <summary>
    /// Gets the CSS class for the article card based on current state.
    /// </summary>
    public string GetCardCssClass()
    {
        return ProcessingPhase switch
        {
            ProcessingPhase.Processing => "image-processing",
            ProcessingPhase.Completed when IsSuccessful => "image-enhanced",
            ProcessingPhase.Failed => "image-failed",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the display text for the current processing state.
    /// </summary>
    public string GetDisplayText()
    {
        return ProcessingPhase switch
        {
            ProcessingPhase.Processing => "Enhancing image...",
            ProcessingPhase.Completed when IsSuccessful => "Enhanced",
            ProcessingPhase.Failed => "Enhancement failed",
            ProcessingPhase.None => string.Empty,
            _ => "Processing..."
        };
    }

    /// <summary>
    /// Gets the FontAwesome icon class for the current state.
    /// </summary>
    public string GetIconClass()
    {
        return ProcessingPhase switch
        {
            ProcessingPhase.Processing => "fas fa-spinner",
            ProcessingPhase.Completed when IsSuccessful => "fas fa-check-circle",
            ProcessingPhase.Failed => "fas fa-exclamation-triangle",
            _ => "fas fa-image"
        };
    }

    /// <summary>
    /// Updates the state to processing phase.
    /// </summary>
    public void StartProcessing()
    {
        ProcessingPhase = ProcessingPhase.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        ProcessingCompletedAt = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Updates the state to completed phase with enhanced image data.
    /// </summary>
    /// <param name="enhancedImage">The enhanced image data</param>
    public void CompleteProcessing(CachedImageData? enhancedImage)
    {
        ProcessingPhase = ProcessingPhase.Completed;
        EnhancedImage = enhancedImage;
        ProcessingCompletedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// Updates the state to failed phase with error information.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="fallbackReason">The fallback reason</param>
    public void FailProcessing(string errorMessage, string? fallbackReason = null)
    {
        ProcessingPhase = ProcessingPhase.Failed;
        ErrorMessage = errorMessage;
        FallbackReason = fallbackReason;
        ProcessingCompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the image load state.
    /// </summary>
    /// <param name="loadState">The image load state</param>
    public void SetImageLoadState(ImageLoadState loadState)
    {
        ImageLoadState = loadState;
    }

    /// <summary>
    /// Sets the fallback reason.
    /// </summary>
    /// <param name="fallbackReason">The fallback reason</param>
    public void SetFallbackReason(string fallbackReason)
    {
        FallbackReason = fallbackReason;
    }

    /// <summary>
    /// Increments the retry count for failed processing.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
    }

    /// <summary>
    /// Resets the state for retry processing.
    /// </summary>
    public void ResetForRetry()
    {
        ProcessingPhase = ProcessingPhase.None;
        ProcessingStartedAt = DateTime.UtcNow;
        ProcessingCompletedAt = null;
        ErrorMessage = null;
        FallbackReason = null;
    }
}

/// <summary>
/// Defines the main processing phases for article image processing.
/// </summary>
public enum ProcessingPhase
{
    /// <summary>
    /// No processing has been initiated.
    /// </summary>
    None,

    /// <summary>
    /// Article is queued for processing.
    /// </summary>
    Queued,

    /// <summary>
    /// Article is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Processing completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Processing failed.
    /// </summary>
    Failed
}

/// <summary>
/// Defines the image loading states for client-side image display.
/// </summary>
public enum ImageLoadState
{
    /// <summary>
    /// Image loading state is unknown.
    /// </summary>
    None,

    /// <summary>
    /// Image is currently loading.
    /// </summary>
    Loading,

    /// <summary>
    /// Image loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    /// Image failed to load.
    /// </summary>
    Failed
}

/// <summary>
/// Defines the OpenGraph processing states for server-side image retrieval.
/// </summary>
public enum OpenGraphProcessingState
{
    /// <summary>
    /// OpenGraph processing not started.
    /// </summary>
    None,

    /// <summary>
    /// Fetching HTML content from the article URL.
    /// </summary>
    FetchingHtml,

    /// <summary>
    /// Parsing OpenGraph metadata from HTML.
    /// </summary>
    ParsingMetadata,

    /// <summary>
    /// Extracting image URLs from metadata.
    /// </summary>
    ExtractingImages,

    /// <summary>
    /// OpenGraph processing completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// OpenGraph processing failed.
    /// </summary>
    Failed
}

/// <summary>
/// Defines the image validation states for image URL verification.
/// </summary>
public enum ImageValidationState
{
    /// <summary>
    /// Image validation not started.
    /// </summary>
    None,

    /// <summary>
    /// Validating image URL accessibility.
    /// </summary>
    Validating,

    /// <summary>
    /// Image validation completed successfully.
    /// </summary>
    Validated,

    /// <summary>
    /// Image validation failed.
    /// </summary>
    Failed
}
