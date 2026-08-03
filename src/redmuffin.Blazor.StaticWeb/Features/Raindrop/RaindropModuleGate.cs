using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop;

/// <summary>
/// Holds the lazy-loaded Raindrop facade after navigate-time load.
/// Registered at host startup so MS.DI stays fixed after <c>Build()</c>.
/// </summary>
public sealed class RaindropModuleGate
{
    private IRaindropItemsFacade? _facade;

    public bool IsReady => _facade is not null;

    public void SetFacade(IRaindropItemsFacade facade)
    {
        ArgumentNullException.ThrowIfNull(facade);
        _facade = facade;
    }

    public IRaindropItemsFacade GetRequiredFacade()
    {
        return _facade
            ?? throw new InvalidOperationException(
                "Raindrop module is not loaded. Open /articles or /videos to load it before resolving IRaindropItemsFacade.");
    }
}
