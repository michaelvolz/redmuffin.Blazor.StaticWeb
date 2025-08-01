namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Enums;

/// <summary>
///     Represents the status of a raindrop cache operation.
/// </summary>
public enum RaindropCacheStatus
{
    /// <summary>
    ///     Cache hit - data was found and is valid.
    /// </summary>
    Hit,

    /// <summary>
    ///     Cache miss - data was not found in cache.
    /// </summary>
    Miss,

    /// <summary>
    ///     Cache expired - data was found but has expired.
    /// </summary>
    Expired,

    /// <summary>
    ///     Cache error - an error occurred during cache operation.
    /// </summary>
    Error
}