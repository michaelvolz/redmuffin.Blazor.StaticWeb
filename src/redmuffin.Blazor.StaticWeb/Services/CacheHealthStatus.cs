namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Cache health status enumeration.
/// </summary>
public enum CacheHealthStatus
{
    /// <summary>
    ///     Cache is healthy and performing well.
    /// </summary>
    Healthy,

    /// <summary>
    ///     Cache has some performance issues but is functional.
    /// </summary>
    Warning,

    /// <summary>
    ///     Cache has significant performance issues.
    /// </summary>
    Critical,

    /// <summary>
    ///     Cache is experiencing severe problems.
    /// </summary>
    Error
}