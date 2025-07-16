namespace redmuffin.Blazor.StaticWeb.Services;

/// <summary>
///     Wrapper for cached items with metadata.
/// </summary>
/// <typeparam name="T">Type of the cached value</typeparam>
internal sealed class CacheEntry<T>
{
    public T Value { get; set; } = default!;
    public DateTime CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
}