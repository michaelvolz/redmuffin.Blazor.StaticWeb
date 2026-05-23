namespace redmuffin.Blazor.StaticWeb.Features.Common.PageLoadSpeed.Models;

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

    /// <summary>Muted neutral for diagnostic values with no industry judgment basis (raw data).</summary>
    public const string DiagnosticMuted = "#666";

    /// <summary>WASM download time ≤600ms — CDN is healthy.</summary>
    public static string ForWasmDownload(double ms) =>
        ms <= 600 ? LighthouseGood :
        ms <= 2000 ? LighthouseWarning :
        LighthousePoor;

    /// <summary>Assembly count ≤70 — trimming is working. >150 means trimming failed.</summary>
    public static string ForAssemblyCount(int count) =>
        count <= 70 ? LighthouseGood :
        count <= 150 ? LighthouseWarning :
        LighthousePoor;

    /// <summary>WASM memory ≤50 MB — normal. >100 MB means potential leak.</summary>
    public static string ForWasmMemory(double mb) =>
        mb <= 50 ? LighthouseGood :
        mb <= 100 ? LighthouseWarning :
        LighthousePoor;

    /// <summary>Runtime startup ≤1000ms — normal. >2000ms means something wrong on production.</summary>
    public static string ForRuntimeStartup(double ms) =>
        ms <= 1000 ? LighthouseGood :
        ms <= 2000 ? LighthouseWarning :
        LighthousePoor;

    /// <summary>Blazor init ≤3000ms — normal. >5000ms means app hung.</summary>
    public static string ForBlazorInit(double ms) =>
        ms <= 3000 ? LighthouseGood :
        ms <= 5000 ? LighthouseWarning :
        LighthousePoor;
}
