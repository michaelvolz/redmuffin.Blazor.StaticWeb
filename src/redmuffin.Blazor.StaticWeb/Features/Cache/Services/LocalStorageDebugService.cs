using System.Globalization;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Features.Cache.Models;

namespace redmuffin.Blazor.StaticWeb.Features.Cache.Services;

/// <summary>
///     Debug service to help diagnose localStorage issues.
/// </summary>
public class LocalStorageDebugService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageDebugService> _logger;

    public LocalStorageDebugService(
        ILocalStorageService localStorage,
        IJSRuntime jsRuntime,
        ILogger<LocalStorageDebugService> logger)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    ///     Performs comprehensive localStorage diagnostics.
    /// </summary>
    public async Task<LocalStorageDiagnostics> DiagnoseLocalStorageAsync(CancellationToken cancellationToken = default)
    {
        var diagnostics = new LocalStorageDiagnostics();

        try
        {
            // Test basic localStorage availability
            diagnostics.IsLocalStorageAvailable = await TestLocalStorageAvailabilityAsync().ConfigureAwait(false);
            
            // Test Blazored.LocalStorage service
            diagnostics.IsBlazoredServiceWorking = await TestBlazoredServiceAsync(cancellationToken).ConfigureAwait(false);
            
            // Get storage info
            diagnostics.StorageInfo = await GetStorageInfoAsync().ConfigureAwait(false);
            
            // Test JSON serialization
            diagnostics.JsonSerializationWorks = await TestJsonSerializationAsync(cancellationToken).ConfigureAwait(false);
            
            // Check existing cache keys
            diagnostics.ExistingCacheKeys = await GetExistingCacheKeysAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("LocalStorage diagnostics completed: Available={IsAvailable}, BlazoredWorking={BlazoredWorking}, UsedStorage={UsedBytes}MB",
                diagnostics.IsLocalStorageAvailable,
                diagnostics.IsBlazoredServiceWorking,
                (diagnostics.StorageInfo?.UsedBytes / (1024.0 * 1024.0) ?? 0).ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run localStorage diagnostics");
            diagnostics.DiagnosticError = ex.Message;
        }

        return diagnostics;
    }

    private async Task<bool> TestLocalStorageAvailabilityAsync()
    {
        try
        {
            // Direct JavaScript localStorage test
            return await _jsRuntime.InvokeAsync<bool>(
                "eval",
                @"(() => {
                    try {
                        const testKey = '__test_localStorage_' + Date.now();
                        localStorage.setItem(testKey, 'test');
                        const result = localStorage.getItem(testKey) === 'test';
                        localStorage.removeItem(testKey);
                        return result;
                    } catch (e) {
                        console.error('localStorage test failed:', e);
                        return false;
                    }
                })()").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "localStorage availability test failed");
            return false;
        }
    }

    private async Task<bool> TestBlazoredServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var testKey = "__blazored_test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            var testValue = "test_value_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

            // Test set
            await _localStorage.SetItemAsync(testKey, testValue, cancellationToken).ConfigureAwait(false);
            
            // Test get
            var retrieved = await _localStorage.GetItemAsync<string>(testKey, cancellationToken).ConfigureAwait(false);
            
            // Test remove
            await _localStorage.RemoveItemAsync(testKey, cancellationToken).ConfigureAwait(false);

            var success = string.Equals(testValue, retrieved, StringComparison.Ordinal);
            
            if (!success)
            {
                _logger.LogWarning("Blazored localStorage test failed: expected '{Expected}', got '{Actual}'", testValue, retrieved);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blazored localStorage service test failed");
            return false;
        }
    }

    private async Task<StorageInfo?> GetStorageInfoAsync()
    {
        try
        {
            var storageInfo = await _jsRuntime.InvokeAsync<StorageInfo>(
                "eval",
                @"(() => {
                    try {
                        const estimate = navigator.storage && navigator.storage.estimate 
                            ? navigator.storage.estimate() 
                            : Promise.resolve({ quota: 10 * 1024 * 1024, usage: 0 });
                        
                        return estimate.then(est => ({
                            quotaBytes: est.quota || 10 * 1024 * 1024,
                            usedBytes: est.usage || 0,
                            localStorageLength: localStorage.length,
                            localStorageSize: JSON.stringify(localStorage).length
                        }));
                    } catch (e) {
                        console.error('Storage info failed:', e);
                        return {
                            quotaBytes: 10 * 1024 * 1024,
                            usedBytes: 0,
                            localStorageLength: 0,
                            localStorageSize: 0
                        };
                    }
                })()").ConfigureAwait(false);

            return storageInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage info");
            return null;
        }
    }

    private async Task<bool> TestJsonSerializationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var testKey = "__json_test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            var testObject = new { Name = "Test", Value = 123, Date = DateTime.UtcNow };

            await _localStorage.SetItemAsStringAsync(testKey, System.Text.Json.JsonSerializer.Serialize(testObject), cancellationToken).ConfigureAwait(false);
            var retrieved = await _localStorage.GetItemAsStringAsync(testKey, cancellationToken).ConfigureAwait(false);
            await _localStorage.RemoveItemAsync(testKey, cancellationToken).ConfigureAwait(false);

            return !string.IsNullOrEmpty(retrieved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON serialization test failed");
            return false;
        }
    }

    private async Task<IList<string>> GetExistingCacheKeysAsync(CancellationToken cancellationToken)
    {
        try
        {
            var allKeys = await _localStorage.KeysAsync(cancellationToken).ConfigureAwait(false);
            return allKeys.Where(key => key.Contains("raindrop_cache_") || key.Contains("img_validation_")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get existing cache keys");
            return new List<string>();
        }
    }
}

