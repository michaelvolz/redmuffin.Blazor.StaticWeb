namespace redmuffin.Blazor.StaticWeb.Common.Enums;

/// <summary>
///     Represents different types of errors that can occur during image retrieval
/// </summary>
public enum ImageRetrievalErrorType
{
    /// <summary>
    ///     No error occurred
    /// </summary>
    None = 0,

    /// <summary>
    ///     Invalid URL provided
    /// </summary>
    InvalidUrl = 1,

    /// <summary>
    ///     Cache service is unavailable or threw an exception
    /// </summary>
    CacheServiceError = 2,

    /// <summary>
    ///     API call failed (network, timeout, etc.)
    /// </summary>
    ApiError = 3,

    /// <summary>
    ///     Image validation failed
    /// </summary>
    ValidationError = 4,

    /// <summary>
    ///     Operation was cancelled
    /// </summary>
    Cancelled = 5,

    /// <summary>
    ///     Request timeout
    /// </summary>
    Timeout = 6,

    /// <summary>
    ///     Rate limit exceeded
    /// </summary>
    RateLimitExceeded = 7,

    /// <summary>
    ///     Unknown or unexpected error
    /// </summary>
    Unknown = 99,
}