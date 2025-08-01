namespace redmuffin.Blazor.StaticWeb.Features.Cache.Services;

/// <summary>
///     LoggerMessage delegates for RaindropItemsCache.
/// </summary>
public sealed partial class RaindropItemsCache
{
    private static readonly Action<ILogger, string, Exception?> LogCacheRetrievalStarted =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(LogCacheRetrievalStarted)),
            "Starting cache retrieval for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogCacheNotFound =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(LogCacheNotFound)),
            "Cache not found for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogCacheMetadataCorrupted =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3, nameof(LogCacheMetadataCorrupted)),
            "Cache metadata corrupted for cache type: {CacheType}");

    private static readonly Action<ILogger, string, DateTime, Exception?> LogCacheExpired =
        LoggerMessage.Define<string, DateTime>(
            LogLevel.Information,
            new EventId(4, nameof(LogCacheExpired)),
            "Cache expired for cache type: {CacheType}, created at: {CreatedAt}");

    private static readonly Action<ILogger, string, Exception?> LogCacheDataCorrupted =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5, nameof(LogCacheDataCorrupted)),
            "Cache data corrupted for cache type: {CacheType}");

    private static readonly Action<ILogger, string, int, Exception?> LogCacheRetrievalSuccessful =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(6, nameof(LogCacheRetrievalSuccessful)),
            "Cache retrieval successful for cache type: {CacheType}, item count: {ItemCount}");

    private static readonly Action<ILogger, string, Exception?> LogCacheRetrievalFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7, nameof(LogCacheRetrievalFailed)),
            "Cache retrieval failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, int, Exception?> LogCacheStorageStarted =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8, nameof(LogCacheStorageStarted)),
            "Starting cache storage for cache type: {CacheType}, item count: {ItemCount}");

    private static readonly Action<ILogger, string, int, int, int, double, Exception?> LogCacheStorageSuccessful =
        LoggerMessage.Define<string, int, int, int, double>(
            LogLevel.Information,
            new EventId(9, nameof(LogCacheStorageSuccessful)),
            "Cache storage successful for cache type: {CacheType}, item count: {ItemCount}, original size: {OriginalSize} bytes, compressed size: {CompressedSize} bytes, compression ratio: {CompressionRatio:P1}");

    private static readonly Action<ILogger, string, int, Exception?> LogCacheStorageFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Error,
            new EventId(10, nameof(LogCacheStorageFailed)),
            "Cache storage failed for cache type: {CacheType}, item count: {ItemCount}");

    private static readonly Action<ILogger, string, Exception?> LogCacheClearStarted =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(11, nameof(LogCacheClearStarted)),
            "Starting cache clear for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogCacheClearSuccessful =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(12, nameof(LogCacheClearSuccessful)),
            "Cache clear successful for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogCacheClearFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(13, nameof(LogCacheClearFailed)),
            "Cache clear failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogCacheExpirationCheckFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(14, nameof(LogCacheExpirationCheckFailed)),
            "Cache expiration check failed for cache type: {CacheType}");

    private static readonly Action<ILogger, Exception?> LogCacheClearAllStarted =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(15, nameof(LogCacheClearAllStarted)),
            "Starting clear all caches operation");

    private static readonly Action<ILogger, Exception?> LogCacheClearAllSuccessful =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(16, nameof(LogCacheClearAllSuccessful)),
            "Clear all caches operation successful");

    private static readonly Action<ILogger, Exception?> LogCacheClearAllFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(17, nameof(LogCacheClearAllFailed)),
            "Clear all caches operation failed");

    private static readonly Action<ILogger, string, Exception?> LogCompressionFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(18, nameof(LogCompressionFailed)),
            "Data compression failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogDecompressionFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(19, nameof(LogDecompressionFailed)),
            "Data decompression failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogSerializationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(20, nameof(LogSerializationFailed)),
            "Data serialization failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogDeserializationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(21, nameof(LogDeserializationFailed)),
            "Data deserialization failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogLocalStorageOperationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(22, nameof(LogLocalStorageOperationFailed)),
            "LocalStorage operation failed for cache type: {CacheType}");

    private static readonly Action<ILogger, string, Exception?> LogLastAccessTimeUpdateFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(23, nameof(LogLastAccessTimeUpdateFailed)),
            "Failed to update last accessed time for cache type: {CacheType}");
}