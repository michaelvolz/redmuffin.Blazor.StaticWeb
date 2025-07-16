using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.CacheResetPage;

public partial class CacheReset
{
    private bool _isProcessing;
    private bool _resetCompleted;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private int _itemsCleared;

    [Inject] private IBrowserStorageService BrowserStorageService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<CacheReset> Logger { get; set; } = default!;

    private static readonly Action<ILogger, Exception?> LogCacheResetStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogCacheResetStarted)),
            "Starting cache reset operation");

    private static readonly Action<ILogger, int, Exception?> LogCacheResetCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, nameof(LogCacheResetCompleted)),
            "Cache reset completed successfully. Items cleared: {ItemsCleared}");

    private static readonly Action<ILogger, Exception?> LogCacheResetError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogCacheResetError)),
            "Error occurred during cache reset");

    private async Task ConfirmResetAsync()
    {
        await PerformCacheResetAsync().ConfigureAwait(false);
    }

    private async Task TryResetAgainAsync()
    {
        _hasError = false;
        _errorMessage = string.Empty;
        await PerformCacheResetAsync().ConfigureAwait(false);
    }

    private async Task ResetAgainAsync()
    {
        _resetCompleted = false;
        _itemsCleared = 0;
        await PerformCacheResetAsync().ConfigureAwait(false);
    }

    private async Task PerformCacheResetAsync()
    {
        _isProcessing = true;
        _hasError = false;
        _errorMessage = string.Empty;

        try
        {
            LogCacheResetStarted(Logger, null);

            // Clear all localStorage data
            _itemsCleared = await BrowserStorageService.ClearAllStorageAsync().ConfigureAwait(false);

            LogCacheResetCompleted(Logger, _itemsCleared, null);

            _resetCompleted = true;
        }
        catch (Exception ex)
        {
            LogCacheResetError(Logger, ex);
            _hasError = true;
            _errorMessage = ex.Message;
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
}
