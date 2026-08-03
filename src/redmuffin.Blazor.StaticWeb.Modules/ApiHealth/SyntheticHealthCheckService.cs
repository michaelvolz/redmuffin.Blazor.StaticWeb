using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

internal sealed class SyntheticHealthCheckService : IHealthCheckService
{
    public Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success("Hello from the ApiHealth module! (synthetic data)"));
    }
}
