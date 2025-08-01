using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using LZStringCSharp;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Cache.Services;

/// <summary>
///     Implementation of raindrop items cache using LocalStorage with compression and expiration support.
/// </summary>
public sealed partial class RaindropItemsCache : IRaindropItemsCache
{
    private const string CacheKeyPrefix = "raindrop_cache_";
    private const string MetadataKeySuffix = "_metadata";
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<RaindropItemsCache> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RaindropItemsCache(
        ILocalStorageService localStorage,
        ILogger<RaindropItemsCache> logger)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private static RaindropCacheMetadata CreateCacheMetadata(int itemCount, int originalSize, int compressedSize)
    {
        var metadata = RaindropCacheMetadata.Create("1.0");
        metadata.ItemCount = itemCount;
        metadata.OriginalSize = originalSize;
        metadata.CompressedSize = compressedSize;
        return metadata;
    }

    private static string GetCacheTypeFromKey(string cacheKey)
    {
        return cacheKey.StartsWith(CacheKeyPrefix, StringComparison.Ordinal)
            ? cacheKey.Substring(CacheKeyPrefix.Length)
            : cacheKey;
    }

    private static string GetCacheKey(string cacheType)
    {
        return $"{CacheKeyPrefix}{cacheType}";
    }

    private static string GetMetadataKey(string cacheType)
    {
        return $"{CacheKeyPrefix}{cacheType}{MetadataKeySuffix}";
    }

    public async Task<RaindropCacheResult<IList<RaindropItem>>> GetAsync(
        string cacheType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheType);

        try
        {
            LogCacheRetrievalStarted(_logger, cacheType, null);

            var metadata = await ValidateAndRetrieveMetadataAsync(cacheType, cancellationToken).ConfigureAwait(false);
            if (metadata == null)
            {
                return RaindropCacheResultFactory.Miss<IList<RaindropItem>>();
            }

            if (metadata.IsExpired)
            {
                LogCacheExpired(_logger, cacheType, metadata.CreatedAt.DateTime, null);
                return RaindropCacheResultFactory.Expired<IList<RaindropItem>>();
            }

            var compressedData = await RetrieveCompressedDataAsync(cacheType, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(compressedData))
            {
                LogCacheDataCorrupted(_logger, cacheType, null);
                return RaindropCacheResultFactory.Miss<IList<RaindropItem>>();
            }

            var cachedData = await DecompressAndDeserializeDataAsync(cacheType, compressedData).ConfigureAwait(false);
            if (cachedData == null)
            {
                return RaindropCacheResultFactory.Error<IList<RaindropItem>>("Data processing failed");
            }

            await UpdateLastAccessedTimeAsync(cacheType, metadata, cancellationToken).ConfigureAwait(false);

            LogCacheRetrievalSuccessful(_logger, cacheType, cachedData.Count, null);
            return RaindropCacheResultFactory.Success(cachedData, metadata);
        }
        catch (Exception ex)
        {
            LogCacheRetrievalFailed(_logger, cacheType, ex);
            return RaindropCacheResultFactory.Error<IList<RaindropItem>>(ex.Message);
        }
    }

    private async Task<RaindropCacheMetadata?> ValidateAndRetrieveMetadataAsync(
        string cacheType,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadataKey = GetMetadataKey(cacheType);

            var metadataExists = await _localStorage.ContainKeyAsync(metadataKey, cancellationToken).ConfigureAwait(false);
            if (!metadataExists)
            {
                LogCacheNotFound(_logger, cacheType, null);
                return null;
            }

            var metadata = await _localStorage.GetItemAsync<RaindropCacheMetadata>(metadataKey, cancellationToken).ConfigureAwait(false);
            if (metadata == null)
            {
                LogCacheMetadataCorrupted(_logger, cacheType, null);
                return null;
            }

            return metadata;
        }
        catch (Exception ex)
        {
            LogLocalStorageOperationFailed(_logger, cacheType, ex);
            throw;
        }
    }

    private async Task<string?> RetrieveCompressedDataAsync(
        string cacheType,
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = GetCacheKey(cacheType);
            return await _localStorage.GetItemAsync<string>(cacheKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogLocalStorageOperationFailed(_logger, cacheType, ex);
            throw;
        }
    }

    private Task<IList<RaindropItem>?> DecompressAndDeserializeDataAsync(
        string cacheType,
        string compressedData)
    {
        try
        {
            var decompressedJson = LZString.DecompressFromUTF16(compressedData);
            if (string.IsNullOrEmpty(decompressedJson))
             {
                 LogDecompressionFailed(_logger, cacheType, null);
                 return Task.FromResult<IList<RaindropItem>?>(null);
             }

            var cachedData = JsonSerializer.Deserialize<IList<RaindropItem>>(decompressedJson, _jsonOptions);
            if (cachedData == null)
             {
                 LogDeserializationFailed(_logger, cacheType, null);
                 return Task.FromResult<IList<RaindropItem>?>(null);
             }

            return Task.FromResult<IList<RaindropItem>?>(cachedData);
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            LogDecompressionFailed(_logger, cacheType, ex);
            throw;
        }
        catch (JsonException ex)
        {
            LogDeserializationFailed(_logger, cacheType, ex);
            return Task.FromResult<IList<RaindropItem>?>(null);
        }
    }

    private async Task UpdateLastAccessedTimeAsync(
        string cacheType,
        RaindropCacheMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            metadata.UpdateLastAccessed();
            var metadataKey = GetMetadataKey(cacheType);
            await _localStorage.SetItemAsync(metadataKey, metadata, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogLastAccessTimeUpdateFailed(_logger, cacheType, ex);
        }
    }

    public async Task SetAsync(
        string cacheType,
        IList<RaindropItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheType);
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            var cacheKey = GetCacheKey(cacheType);
            var metadataKey = GetMetadataKey(cacheType);

            LogCacheStorageStarted(_logger, cacheType, items.Count, null);

            var (compressedData, originalSize, compressedSize) = await SerializeAndCompressDataAsync(cacheType, items).ConfigureAwait(false);
            var metadata = CreateCacheMetadata(items.Count, originalSize, compressedSize);

            await StoreDataAndMetadataAsync(cacheKey, metadataKey, compressedData, metadata, cancellationToken).ConfigureAwait(false);

            LogCacheStorageSuccessful(_logger, cacheType, items.Count, originalSize, compressedSize, metadata.CompressionRatio, null);
        }
        catch (Exception ex)
        {
            LogCacheStorageFailed(_logger, cacheType, items.Count, ex);
            throw;
        }
    }

    public async Task ClearAsync(string cacheType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheType);

        try
        {
            var cacheKey = GetCacheKey(cacheType);
            var metadataKey = GetMetadataKey(cacheType);

            LogCacheClearStarted(_logger, cacheType, null);

            try
            {
                await _localStorage.RemoveItemAsync(cacheKey, cancellationToken).ConfigureAwait(false);
                await _localStorage.RemoveItemAsync(metadataKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogLocalStorageOperationFailed(_logger, cacheType, ex);
                throw new InvalidOperationException("LocalStorage clear operation failed", ex);
            }

            LogCacheClearSuccessful(_logger, cacheType, null);
        }
        catch (Exception ex)
        {
            LogCacheClearFailed(_logger, cacheType, ex);
            throw;
        }
    }

    public async Task<bool> IsExpiredAsync(string cacheType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheType);

        try
        {
            var metadataKey = GetMetadataKey(cacheType);

            bool metadataExists;
            try
            {
                metadataExists = await _localStorage.ContainKeyAsync(metadataKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogLocalStorageOperationFailed(_logger, cacheType, ex);
                return true; // Error means treat as expired
            }

            if (!metadataExists) return true; // No cache means expired

            RaindropCacheMetadata? metadata;
            try
            {
                metadata = await _localStorage.GetItemAsync<RaindropCacheMetadata>(metadataKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogLocalStorageOperationFailed(_logger, cacheType, ex);
                return true; // Error means treat as expired
            }

            if (metadata == null) return true; // Corrupted metadata means expired

            return metadata.IsExpired;
        }
        catch (Exception ex)
        {
            LogCacheExpirationCheckFailed(_logger, cacheType, ex);
            return true; // Error means treat as expired
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LogCacheClearAllStarted(_logger, null);

            // Clear both Videos and Articles caches
            await ClearAsync("Videos", cancellationToken).ConfigureAwait(false);
            await ClearAsync("Articles", cancellationToken).ConfigureAwait(false);

            LogCacheClearAllSuccessful(_logger, null);
        }
        catch (Exception ex)
        {
            LogCacheClearAllFailed(_logger, ex);
            throw;
        }
    }

    private Task<(string CompressedData, int OriginalSize, int CompressedSize)> SerializeAndCompressDataAsync(
        string cacheType,
        IList<RaindropItem> items)
    {
        // Serialize data
        string jsonData;
        try
        {
            jsonData = JsonSerializer.Serialize(items, _jsonOptions);
        }
        catch (Exception ex)
        {
            LogSerializationFailed(_logger, cacheType, ex);
            throw new InvalidOperationException("Data serialization failed", ex);
        }

        var originalSize = Encoding.UTF8.GetByteCount(jsonData);

        // Compress data
        string compressedData;
        try
        {
            compressedData = LZString.CompressToUTF16(jsonData);
            if (string.IsNullOrEmpty(compressedData))
            {
                LogCompressionFailed(_logger, cacheType, null);
                throw new InvalidOperationException("Data compression failed - result is null or empty");
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            LogCompressionFailed(_logger, cacheType, ex);
            throw new InvalidOperationException("Data compression failed", ex);
        }

        var compressedSize = Encoding.UTF8.GetByteCount(compressedData);
        return Task.FromResult((CompressedData: compressedData, OriginalSize: originalSize, CompressedSize: compressedSize));
    }

    private async Task StoreDataAndMetadataAsync(
        string cacheKey,
        string metadataKey,
        string compressedData,
        RaindropCacheMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            await _localStorage.SetItemAsync(cacheKey, compressedData, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogLocalStorageOperationFailed(_logger, GetCacheTypeFromKey(cacheKey), ex);
            throw new InvalidOperationException("LocalStorage operation failed", ex);
        }

        // Try to store metadata, but don't fail the entire operation if this fails
        try
        {
            await _localStorage.SetItemAsync(metadataKey, metadata, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log warning but continue - metadata storage failure shouldn't prevent cache storage
            LogLocalStorageOperationFailed(_logger, GetCacheTypeFromKey(cacheKey), ex);
        }
    }
}