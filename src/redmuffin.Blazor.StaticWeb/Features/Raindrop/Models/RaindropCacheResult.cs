using redmuffin.Blazor.StaticWeb.Features.Raindrop.Enums;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;

/// <summary>
///     Represents the result of a raindrop cache operation with success/failure states and optional data.
/// </summary>
/// <typeparam name="T">The type of raindrop data being cached.</typeparam>
public sealed class RaindropCacheResult<T>
{
    /// <summary>
    ///     Gets the status of the cache operation.
    /// </summary>
    public RaindropCacheStatus Status { get; init; }

    /// <summary>
    ///     Gets the cached data if the operation was successful.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    ///     Gets the cache metadata including timestamps and version information.
    /// </summary>
    public RaindropCacheMetadata? Metadata { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the cache operation was successful.
    /// </summary>
    public bool IsSuccess => Status == RaindropCacheStatus.Hit;

    /// <summary>
    ///     Gets a value indicating whether the cache was expired.
    /// </summary>
    public bool IsExpired => Status == RaindropCacheStatus.Expired;

    /// <summary>
    ///     Gets a value indicating whether the cache was not found.
    /// </summary>
    public bool IsMiss => Status == RaindropCacheStatus.Miss;
}