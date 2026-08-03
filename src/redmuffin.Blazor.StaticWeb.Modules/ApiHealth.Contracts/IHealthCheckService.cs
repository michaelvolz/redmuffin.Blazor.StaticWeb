using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

public interface IHealthCheckService
{
    Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default);
}
