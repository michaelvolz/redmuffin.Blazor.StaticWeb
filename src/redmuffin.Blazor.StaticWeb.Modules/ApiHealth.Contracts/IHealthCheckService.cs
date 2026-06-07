namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

public interface IHealthCheckService
{
    Task<string> GetHelloAsync(CancellationToken cancellationToken = default);
}
