using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck;

internal sealed class SyntheticHealthCheckService : IHealthCheckService
{
    public Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success("Hello from the AzureHealthCheck module! (synthetic data)"));
    }
}
