using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;

/// <summary>
/// Eager Mediator handler so SourceGen does not root the lazy AzureHealthCheck
/// implementation assembly at host boot. Depends only on
/// <see cref="IHealthCheckService"/> resolved via <see cref="AzureHealthCheckModuleGate"/>.
/// </summary>
public sealed class GetHelloHandler : IRequestHandler<GetHelloQuery, Result<HelloResponse>>
{
    private readonly IHealthCheckService _healthCheckService;

    public GetHelloHandler(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }

    public async ValueTask<Result<HelloResponse>> Handle(GetHelloQuery request, CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.GetHelloAsync(cancellationToken).ConfigureAwait(false);
        return result.Match(
            message => Result.Success(new HelloResponse(message)),
            error => Result.Failure<HelloResponse>(error));
    }
}
