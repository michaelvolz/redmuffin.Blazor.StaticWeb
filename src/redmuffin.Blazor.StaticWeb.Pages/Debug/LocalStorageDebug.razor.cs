using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Pages.Debug.Models;
using redmuffin.Blazor.StaticWeb.Pages.Debug.Services;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug;

public partial class LocalStorageDebug : ComponentBase
{
    private readonly LocalStorageDebugService _debugService;
    private readonly ILogger<LocalStorageDebug> _logger;
    private LocalStorageDiagnostics? _diagnostics;
    private bool _isRunning;

    public LocalStorageDebug(
        ILocalStorageService localStorage,
        IJSRuntime jsRuntime,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(localStorage);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<LocalStorageDebug>();
        _debugService = new LocalStorageDebugService(
            localStorage,
            jsRuntime,
            loggerFactory.CreateLogger<LocalStorageDebugService>());
    }

    protected override Task OnInitializedAsync()
    {
        return RunDiagnosticsAsync();
    }

    private async Task RunDiagnosticsAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _diagnostics = null;
        StateHasChanged();

        try
        {
            _diagnostics = await _debugService.DiagnoseLocalStorageAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailed(_logger, ex);
            _diagnostics = new LocalStorageDiagnostics
            {
                DiagnosticError = ex.Message
            };
        }
        finally
        {
            _isRunning = false;
            StateHasChanged();
        }
    }
}
