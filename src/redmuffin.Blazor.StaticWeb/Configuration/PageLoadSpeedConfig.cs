namespace redmuffin.Blazor.StaticWeb.Configuration;

/// <summary>
///     Configuration settings for the LoadSpeed component
/// </summary>
public static class PageLoadSpeedConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether the LoadSpeed component is enabled.
    ///     Set to true to enable the component, false to disable it entirely
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the LoadSpeed component works on localhost.
    ///     Set to true to enable on localhost, false to disable on localhost
    /// </summary>
    public static bool EnableOnLocalhost { get; set; } = true; // Set to true for your current needs

    /// <summary>
    ///     Gets or sets the delay in milliseconds before automatically loading metrics.
    /// </summary>
    public static int AutoLoadDelayMs { get; set; } = 2000;

    /// <summary>
    ///     Gets or sets the timeout in seconds for JavaScript interop calls.
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

        // If EnableOnLocalhost is false, only show on non-localhost (production)
        return !IsLocalhostHost(new Uri(baseUri).Host);
    }

    /// <summary>
    ///     Determines whether the given host string represents a localhost or
    ///     private network address.
    /// </summary>
    /// <returns></returns>
    public static bool IsLocalhostHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase) ||
               host.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
               host.StartsWith("172.16.", StringComparison.OrdinalIgnoreCase);
    }
}