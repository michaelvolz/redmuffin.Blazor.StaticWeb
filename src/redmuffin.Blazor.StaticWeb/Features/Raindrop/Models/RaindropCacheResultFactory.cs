using redmuffin.Blazor.StaticWeb.Features.Raindrop.Enums;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Models;

/// <summary>
///     Static factory methods for creating RaindropCacheResult instances.
/// </summary>
public static class RaindropCacheResultFactory
{
    /// <summary>
    ///     Creates a successful cache result with data and metadata.
    /// </summary>
    /// <typeparam name="T">The type of data being cached.</typeparam>
    /// <param name="data">The cached data.</param>
    /// <param name="metadata">The cache metadata.</param>
    /// <returns>A successful cache result.</returns>
    public static RaindropCacheResult<T> Success<T>(T data, RaindropCacheMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(metadata);

        return new RaindropCacheResult<T>
        {
            Status = RaindropCacheStatus.Hit,
            Data = data,
            Metadata = metadata
        };
    }

    /// <summary>
    ///     Creates a cache miss result.
    /// </summary>
    /// <typeparam name="T">The type of data being cached.</typeparam>
    /// <returns>A cache miss result.</returns>
    public static RaindropCacheResult<T> Miss<T>()
    {
        return new RaindropCacheResult<T>
        {
            Status = RaindropCacheStatus.Miss
        };
    }

    /// <summary>
    ///     Creates an expired cache result.
    /// </summary>
    /// <typeparam name="T">The type of data being cached.</typeparam>
    /// <returns>An expired cache result.</returns>
    public static RaindropCacheResult<T> Expired<T>()
    {
        return new RaindropCacheResult<T>
        {
            Status = RaindropCacheStatus.Expired
        };
    }

    /// <summary>
    ///     Creates a failed cache result with an error message.
    /// </summary>
    /// <typeparam name="T">The type of data being cached.</typeparam>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed cache result.</returns>
    public static RaindropCacheResult<T> Error<T>(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new RaindropCacheResult<T>
        {
            Status = RaindropCacheStatus.Error,
            ErrorMessage = errorMessage
        };
    }
}