using System.Globalization;
using System.Text;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Templates;

/// <summary>
/// Template generator for SVG placeholders.
/// </summary>
public static class SvgPlaceholderTemplate
{
    /// <summary>
    /// Generates an SVG placeholder with the specified text and configuration.
    /// </summary>
    /// <param name="text">The text to display in the placeholder</param>
    /// <param name="configuration">The placeholder configuration</param>
    /// <returns>Base64-encoded SVG data URI</returns>
    public static string Generate(string text, PlaceholderConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        configuration ??= new PlaceholderConfiguration();

        var svg = $@"<svg width=""{configuration.Width.ToString(CultureInfo.InvariantCulture)}"" height=""{configuration.Height.ToString(CultureInfo.InvariantCulture)}"" xmlns=""http://www.w3.org/2000/svg"">
  <rect width=""100%"" height=""100%"" fill=""{configuration.BackgroundColor}"" stroke=""{configuration.BorderColor}"" stroke-width=""{configuration.BorderWidth.ToString(CultureInfo.InvariantCulture)}""/>
  <text x=""50%"" y=""50%"" dominant-baseline=""middle"" text-anchor=""middle"" font-family=""{configuration.FontFamily}"" font-size=""{configuration.FontSize.ToString(CultureInfo.InvariantCulture)}"" fill=""{configuration.TextColor}"">{text}</text>
</svg>";

        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    /// <summary>
    /// Generates a default SVG placeholder with standard "No Image Available" text.
    /// </summary>
    /// <returns>Base64-encoded SVG data URI</returns>
    public static string GenerateDefault()
    {
        var configuration = new PlaceholderConfiguration();
        return Generate(configuration.DefaultText, configuration);
    }

    /// <summary>
    /// Maps failure reasons to user-friendly display text.
    /// </summary>
    /// <param name="reason">The failure reason</param>
    /// <returns>User-friendly display text</returns>
    public static string MapFailureReasonToDisplayText(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return reason switch
        {
            var r when r.Contains("CORS", StringComparison.OrdinalIgnoreCase) => "CORS blocked",
            var r when r.Contains("404", StringComparison.OrdinalIgnoreCase) => "Image not found",
            var r when r.Contains("timeout", StringComparison.OrdinalIgnoreCase) => "Network error",
            var r when r.Contains("content type", StringComparison.OrdinalIgnoreCase) => "Invalid format",
            _ => "Image not available"
        };
    }
}