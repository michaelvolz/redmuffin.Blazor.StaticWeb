using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

/// <summary>
///     Metadata for raindrop cache entries including timestamps and version tracking.
/// </summary>
public sealed class RaindropCacheMetadata
{
    /// <summary>
    ///     Gets or sets the timestamp when the cache entry was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the cache entry was last accessed.
    /// </summary>
    [JsonPropertyName("lastAccessedAt")]
    public DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the cache entry expires.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    ///     Gets or sets the version of the cache entry for invalidation purposes.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the size of the original data before compression.
    /// </summary>
    [JsonPropertyName("originalSize")]
    public long OriginalSize { get; set; }

    /// <summary>
    ///     Gets or sets the size of the compressed data.
    /// </summary>
    [JsonPropertyName("compressedSize")]
    public long CompressedSize { get; set; }

    /// <summary>
    ///     Gets or sets the number of items in the cache entry.
    /// </summary>
    [JsonPropertyName("itemCount")]
    public int ItemCount { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the cache entry has expired.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    /// <summary>
    ///     Gets the compression ratio as a percentage.
    /// </summary>
    [JsonIgnore]
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize * 100 : 0;

    /// <summary>
    ///     Creates a new cache metadata instance with default values.
    /// </summary>
    /// <param name="version">The cache version.</param>
    /// <param name="ttlWeeks">Time to live in weeks (default: 4).</param>
    /// <returns>A new cache metadata instance.</returns>
    public static RaindropCacheMetadata Create(string version, int ttlWeeks = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ttlWeeks);

        var now = DateTimeOffset.UtcNow;
        return new RaindropCacheMetadata
        {
            CreatedAt = now,
            LastAccessedAt = now,
            ExpiresAt = now.AddDays(ttlWeeks * 7),
            Version = version
        };
    }

    /// <summary>
    ///     Updates the last accessed timestamp to the current time.
    /// </summary>
    public void UpdateLastAccessed()
    {
        LastAccessedAt = DateTimeOffset.UtcNow;
    }
}