using System.Text;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Templates;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

/// <summary>
///     Service for generating image placeholders with various configurations.
/// </summary>
public sealed partial class PlaceholderGenerationService
{
    private readonly ILogger<PlaceholderGenerationService> _logger;
    private readonly PlaceholderConfiguration _defaultConfiguration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PlaceholderGenerationService" /> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public PlaceholderGenerationService(ILogger<PlaceholderGenerationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultConfiguration = new PlaceholderConfiguration();
    }

    /// <summary>
    ///     Generates a simple fallback placeholder when other generation methods fail.
    /// </summary>
    /// <returns>A minimal base64-encoded SVG data URI</returns>
    private static string GenerateFallbackPlaceholder()
    {
        // Minimal SVG that should always work
        const string fallbackSvg =
            "<svg width=\"400\" height=\"200\" xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"100%\" height=\"100%\" fill=\"#f5f5f5\"/><text x=\"50%\" y=\"50%\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"#999\">Image Unavailable</text></svg>";
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(fallbackSvg));
    }

    /// <summary>
    ///     Generates a default placeholder with standard "No Image Available" text.
    /// </summary>
    /// <returns>Base64-encoded SVG data URI</returns>
    public string GenerateDefaultPlaceholder()
    {
        try
        {
            return SvgPlaceholderTemplate.GenerateDefault();
        }
        catch (Exception ex)
        {
            LogPlaceholderGenerationError(_logger, "default", ex);
            return GenerateFallbackPlaceholder();
        }
    }

    /// <summary>
    ///     Generates a placeholder with the specified reason text.
    /// </summary>
    /// <param name="reason">The reason for the placeholder</param>
    /// <returns>Base64-encoded SVG data URI</returns>
    public string GeneratePlaceholderWithReason(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        try
        {
            var displayReason = SvgPlaceholderTemplate.MapFailureReasonToDisplayText(reason);
            return SvgPlaceholderTemplate.Generate(displayReason, _defaultConfiguration);
        }
        catch (Exception ex)
        {
            LogPlaceholderGenerationError(_logger, reason, ex);
            return GenerateFallbackPlaceholder();
        }
    }

    /// <summary>
    ///     Generates a placeholder with custom configuration.
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="configuration">The placeholder configuration</param>
    /// <returns>Base64-encoded SVG data URI</returns>
    public string GenerateCustomPlaceholder(string text, PlaceholderConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(configuration);

        try
        {
            return SvgPlaceholderTemplate.Generate(text, configuration);
        }
        catch (Exception ex)
        {
            LogPlaceholderGenerationError(_logger, text, ex);
            return GenerateFallbackPlaceholder();
        }
    }
}