namespace redmuffin.Blazor.StaticWeb.Core.Services;

public partial class BrowserStorageService
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Evicted LRU item: {Key}, Size: {Size} bytes, LastAccessed: {LastAccessed}")]
    private static partial void LogEvictedLRUItem(ILogger logger, string key, long size, DateTime lastAccessed);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "LRU eviction completed. Evicted {Count} items, freed {FreedSize} bytes")]
    private static partial void LogLRUEvictionCompleted(ILogger logger, int count, long freedSize);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Removed expired cache item: {Key}")]
    private static partial void LogRemovedExpiredItem(ILogger logger, string key);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Cleaned up {ExpiredCount} expired cache items")]
    private static partial void LogCleanedUpExpiredItems(ILogger logger, int expiredCount);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Storage approaching capacity ({Usage:P2}), starting LRU eviction to {Target:P2}")]
    private static partial void LogStorageApproachingCapacity(ILogger logger, double usage, double target);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Storage optimization completed. Expired: {ExpiredCount}, Evicted: {EvictedCount}")]
    private static partial void LogStorageOptimizationCompleted(ILogger logger, int expiredCount, int evictedCount);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Storage optimization completed using only expired item cleanup: {ExpiredCount}")]
    private static partial void LogStorageOptimizationExpiredOnly(ILogger logger, int expiredCount);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "All localStorage data cleared. Total items removed: {ItemCount}")]
    private static partial void LogAllStorageCleared(ILogger logger, int itemCount);
}
