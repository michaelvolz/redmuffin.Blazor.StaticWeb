namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Core;

/// <summary>
///     Named color constants for the page load metrics UI.
///     Each name describes the ROLE the color plays, not what color it is.
///     Change any value here and every reference updates automatically.
/// </summary>
public static class PageLoadColors
{
    /// <summary>Metric passes Google's "Good" threshold.</summary>
    public const string LighthouseGood = "#0cce6b";

    /// <summary>Metric is in Google's "Needs Improvement" range.</summary>
    public const string LighthouseWarning = "#ffa400";

    /// <summary>Metric is in Google's "Poor" range.</summary>
    public const string LighthousePoor = "#ff4e42";

    /// <summary>Section accent for calculated breakdown metrics (server response, DOM processing, resource load).</summary>
    public const string SectionBreakdown = "#ff8c42";

    /// <summary>Section accent for data transfer metrics (transfer, encoded, decoded size).</summary>
    public const string SectionDataTransfer = "#00bfff";

    /// <summary>Section accent for compression ratio within data transfer.</summary>
    public const string SectionCompression = "#ff6b9d";

    /// <summary>Muted neutral for diagnostic values with no industry judgment basis (WASM metrics, raw data).</summary>
    public const string DiagnosticMuted = "#666";
}
