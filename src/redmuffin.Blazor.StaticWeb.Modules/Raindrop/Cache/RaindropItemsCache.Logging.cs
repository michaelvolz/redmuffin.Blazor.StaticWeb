using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Cache;

public sealed partial class RaindropItemsCache
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting cache retrieval for cache type: {CacheType}")]
    private static partial void LogCacheRetrievalStarted(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Cache not found for cache type: {CacheType}")]
    private static partial void LogCacheNotFound(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Cache metadata corrupted for cache type: {CacheType}")]
    private static partial void LogCacheMetadataCorrupted(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Cache expired for cache type: {CacheType}, created at: {CreatedAt}")]
    private static partial void LogCacheExpired(ILogger logger, string cacheType, DateTime createdAt);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Cache data corrupted for cache type: {CacheType}")]
    private static partial void LogCacheDataCorrupted(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Cache retrieval successful for cache type: {CacheType}, item count: {ItemCount}")]
    private static partial void LogCacheRetrievalSuccessful(ILogger logger, string cacheType, int itemCount);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Cache retrieval failed for cache type: {CacheType}")]
    private static partial void LogCacheRetrievalFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Starting cache storage for cache type: {CacheType}, item count: {ItemCount}")]
    private static partial void LogCacheStorageStarted(ILogger logger, string cacheType, int itemCount);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Cache storage successful for cache type: {CacheType}, item count: {ItemCount}, original size: {OriginalSize} bytes, compressed size: {CompressedSize} bytes, compression ratio: {CompressionRatio:P1}")]
    private static partial void LogCacheStorageSuccessful(ILogger logger, string cacheType, int itemCount, int originalSize, int compressedSize, double compressionRatio);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Cache storage failed for cache type: {CacheType}, item count: {ItemCount}")]
    private static partial void LogCacheStorageFailed(ILogger logger, string cacheType, int itemCount, Exception exception);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Starting cache clear for cache type: {CacheType}")]
    private static partial void LogCacheClearStarted(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Cache clear successful for cache type: {CacheType}")]
    private static partial void LogCacheClearSuccessful(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Cache clear failed for cache type: {CacheType}")]
    private static partial void LogCacheClearFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Cache expiration check failed for cache type: {CacheType}")]
    private static partial void LogCacheExpirationCheckFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 15, Level = LogLevel.Debug, Message = "Starting clear all caches operation")]
    private static partial void LogCacheClearAllStarted(ILogger logger);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "Clear all caches operation successful")]
    private static partial void LogCacheClearAllSuccessful(ILogger logger);

    [LoggerMessage(EventId = 17, Level = LogLevel.Error, Message = "Clear all caches operation failed")]
    private static partial void LogCacheClearAllFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 18, Level = LogLevel.Error, Message = "Data compression failed for cache type: {CacheType}")]
    private static partial void LogCompressionFailed(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 28, Level = LogLevel.Error, Message = "Data compression failed for cache type: {CacheType}")]
    private static partial void LogCompressionFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 19, Level = LogLevel.Error, Message = "Data decompression failed for cache type: {CacheType}")]
    private static partial void LogDecompressionFailed(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 29, Level = LogLevel.Error, Message = "Data decompression failed for cache type: {CacheType}")]
    private static partial void LogDecompressionFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 20, Level = LogLevel.Error, Message = "Data serialization failed for cache type: {CacheType}")]
    private static partial void LogSerializationFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 21, Level = LogLevel.Error, Message = "Data deserialization failed for cache type: {CacheType}")]
    private static partial void LogDeserializationFailed(ILogger logger, string cacheType);

    [LoggerMessage(EventId = 30, Level = LogLevel.Error, Message = "Data deserialization failed for cache type: {CacheType}")]
    private static partial void LogDeserializationFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "LocalStorage operation failed for cache type: {CacheType}")]
    private static partial void LogLocalStorageOperationFailed(ILogger logger, string cacheType, Exception exception);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "Failed to update last accessed time for cache type: {CacheType}")]
    private static partial void LogLastAccessTimeUpdateFailed(ILogger logger, string cacheType, Exception exception);
}
