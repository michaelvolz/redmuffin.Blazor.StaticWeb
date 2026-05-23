namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

public sealed partial class PlaceholderGenerationService
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Error generating placeholder for reason: {Reason}")]
    private static partial void LogPlaceholderGenerationError(ILogger logger, string reason, Exception exception);
}
