using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public sealed class SyntheticHealthCheckService : IHealthCheckService
{
    public Task<string> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Hello from the ApiHealth module! (synthetic data)");
    }
}
