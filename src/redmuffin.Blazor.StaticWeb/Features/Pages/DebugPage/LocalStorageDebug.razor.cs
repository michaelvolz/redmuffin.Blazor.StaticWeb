using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Cache.Models;
using redmuffin.Blazor.StaticWeb.Features.Cache.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage;

public partial class LocalStorageDebug : ComponentBase
{
    private LocalStorageDiagnostics? _diagnostics;
    private bool _isRunning;

    [Inject] private LocalStorageDebugService DebugService { get; set; } = default!;
    [Inject] private ILogger<LocalStorageDebug> Logger { get; set; } = default!;

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
            _diagnostics = await DebugService.DiagnoseLocalStorageAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogDiagnosticsFailed(Logger, ex);
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