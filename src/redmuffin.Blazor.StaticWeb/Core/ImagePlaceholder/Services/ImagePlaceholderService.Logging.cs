using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Logging partial class for ImagePlaceholderService containing LoggerMessage delegates.
/// </summary>
internal sealed partial class ImagePlaceholderService
{
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Error handling image load for element {ElementId}, item {ItemLink}: {ErrorMessage}")]
    private static partial void LogImageLoadHandlingError(ILogger logger, string elementId, string itemLink, string errorMessage, Exception exception);
}