using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

// Public: Mediator.SourceGen discovers handlers across project references; MA0182 rejects unused internals.
public sealed class GetHelloHandler : IRequestHandler<GetHelloQuery, Result<HelloResponse>>
{
    private readonly IHealthCheckService _healthCheckService;

    public GetHelloHandler(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async ValueTask<Result<HelloResponse>> Handle(GetHelloQuery request, CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.GetHelloAsync(cancellationToken).ConfigureAwait(false);
        return result.Match(
            message => Result.Success(new HelloResponse(message)),
            error => Result.Failure<HelloResponse>(error));
    }
}
