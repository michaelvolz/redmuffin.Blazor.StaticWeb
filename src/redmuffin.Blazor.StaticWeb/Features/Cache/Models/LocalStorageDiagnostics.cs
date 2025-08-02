namespace redmuffin.Blazor.StaticWeb.Features.Cache.Models;

/// <summary>
///     Results from localStorage diagnostic tests.
/// </summary>
public class LocalStorageDiagnostics
{
    /// <summary>
    ///     Gets or sets a value indicating whether localStorage is available in the browser.
    /// </summary>
    public bool IsLocalStorageAvailable { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the Blazored.LocalStorage service is working.
    /// </summary>
    public bool IsBlazoredServiceWorking { get; set; }

    /// <summary>
    ///     Gets or sets the storage information from the browser.
    /// </summary>
    public StorageInfo? StorageInfo { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether JSON serialization is working.
    /// </summary>
    public bool JsonSerializationWorks { get; set; }

    /// <summary>
    ///     Gets or sets the list of existing cache keys found in localStorage.
    /// </summary>
    public IList<string> ExistingCacheKeys { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets the diagnostic error message if diagnostics failed.
    /// </summary>
    public string? DiagnosticError { get; set; }
}
