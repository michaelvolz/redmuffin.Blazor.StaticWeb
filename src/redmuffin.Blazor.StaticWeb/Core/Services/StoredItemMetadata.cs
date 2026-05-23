namespace redmuffin.Blazor.StaticWeb.Core.Services;

public sealed class StoredItemMetadata
{
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public DateTime? ExpiresAt { get; set; }
}