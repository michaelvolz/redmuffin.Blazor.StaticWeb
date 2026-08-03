using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

public interface IHealthCheckService
{
    Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default);
}
