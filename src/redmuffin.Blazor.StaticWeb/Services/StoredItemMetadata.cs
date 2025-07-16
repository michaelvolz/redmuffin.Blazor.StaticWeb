namespace redmuffin.Blazor.StaticWeb.Services;

internal sealed class StoredItemMetadata
{
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public DateTime? ExpiresAt { get; set; }
}