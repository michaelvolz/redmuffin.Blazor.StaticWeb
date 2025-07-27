namespace redmuffin.Blazor.StaticWeb.Configuration;

/// <summary>
///     Configuration settings for the LoadSpeed component
/// </summary>
public static class PageLoadSpeedConfig
{
    /// <summary>
    ///     Controls whether the LoadSpeed component is enabled
    ///     Set to true to enable the component, false to disable it entirely
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Controls whether the component works on localhost
    ///     Set to true to enable on localhost, false to disable on localhost
    /// </summary>
    public static bool EnableOnLocalhost { get; set; } = false; // Set to true for your current needs

    /// <summary>
    ///     Delay in milliseconds before automatically loading metrics
    /// </summary>
    public static int AutoLoadDelayMs { get; set; } = 2000;

    /// <summary>
    ///     Timeout in seconds for JavaScript interop calls
    /// </summary>
    public static int JsInteropTimeoutSeconds { get; set; } = 5;

    /// <summary>
    ///     Determines if the component should be displayed based on current configuration
    /// </summary>
    /// <param name="baseUri">The base URI of the application</param>
    /// <returns>True if the component should be displayed, false otherwise</returns>
    public static bool ShouldDisplayComponent(string baseUri)
    {
        if (!IsEnabled) return false;

        if (EnableOnLocalhost) return true;

        // Check if running on localhost/development
        var uri = new Uri(baseUri);
        var isLocalhost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.StartsWith("172.16.", StringComparison.OrdinalIgnoreCase);

        // If EnableOnLocalhost is false, only show on non-localhost (production)
        return !isLocalhost;
    }
}