using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;

/// <summary>
/// Holds the lazy-loaded AzureHealthCheck implementation service after navigate-time load.
/// Registered at host startup so MS.DI stays fixed after <c>Build()</c>.
/// </summary>
public sealed class AzureHealthCheckModuleGate
{
    private IHealthCheckService? _service;

    public bool IsReady => _service is not null;

    public void SetService(IHealthCheckService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    public IHealthCheckService GetRequiredService()
    {
        return _service
            ?? throw new InvalidOperationException(
                "AzureHealthCheck module is not loaded. Open /api-health to load it before resolving IHealthCheckService.");
    }
}
