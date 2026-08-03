using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug.CacheReset;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class CacheReset : ComponentBase
#pragma warning restore MA0049
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<CacheReset> _logger;
    private bool _isProcessing;
    private bool _resetCompleted;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private int _itemsCleared;

    public CacheReset(ILocalStorageService localStorage, ILogger<CacheReset> logger)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            LogCacheResetStarted(_logger);

            _itemsCleared = await _localStorage.LengthAsync().ConfigureAwait(false);
            await _localStorage.ClearAsync().ConfigureAwait(false);

            LogCacheResetCompleted(_logger, _itemsCleared);

            _resetCompleted = true;
        }
        catch (Exception ex)
        {
            LogCacheResetError(_logger, ex);
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
