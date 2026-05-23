namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage.Models;

/// <summary>
///     Information about browser storage usage.
/// </summary>
public class StorageInfo
{
    /// <summary>
    ///     Gets or sets the storage quota in bytes.
    /// </summary>
    public long QuotaBytes { get; set; }

    /// <summary>
    ///     Gets or sets the used storage in bytes.
    /// </summary>
    public long UsedBytes { get; set; }

    /// <summary>
    ///     Gets or sets the number of items in localStorage.
    /// </summary>
    public int LocalStorageLength { get; set; }

    /// <summary>
    ///     Gets or sets the size of localStorage content in bytes.
    /// </summary>
    public int LocalStorageSize { get; set; }
}