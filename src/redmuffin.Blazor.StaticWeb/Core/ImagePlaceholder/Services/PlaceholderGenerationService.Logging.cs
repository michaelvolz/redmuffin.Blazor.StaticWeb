namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Logging partial class for PlaceholderGenerationService containing LoggerMessage delegates.
/// </summary>
public sealed partial class PlaceholderGenerationService
{
    /// <summary>
    ///     LoggerMessage delegate for placeholder generation errors.
    /// </summary>
    private static readonly Action<ILogger, string, Exception> LogPlaceholderGenerationError =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogPlaceholderGenerationError)),
            "Error generating placeholder for reason: {Reason}");
}