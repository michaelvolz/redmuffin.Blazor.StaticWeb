using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage.CacheResetPage;

public partial class CacheReset : ComponentBase
{
    private bool _isProcessing;
    private bool _resetCompleted;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private int _itemsCleared;
    [Inject] private IBrowserStorageService BrowserStorageService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<CacheReset> Logger { get; set; } = default!;

    private Task ConfirmResetAsync()
    {
        return PerformCacheResetAsync();
    }

    private Task TryResetAgainAsync()
    {
        _hasError = false;
        _errorMessage = string.Empty;
        return PerformCacheResetAsync();
    }

    private Task ResetAgainAsync()
    {
        _resetCompleted = false;
        _itemsCleared = 0;
        return PerformCacheResetAsync();
    }

    private async Task PerformCacheResetAsync()
    {
        _isProcessing = true;
        _hasError = false;
        _errorMessage = string.Empty;

        try
        {
            LogCacheResetStarted(Logger);

            // Clear all localStorage data
            _itemsCleared = await BrowserStorageService.ClearAllStorageAsync().ConfigureAwait(false);

            LogCacheResetCompleted(Logger, _itemsCleared);

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