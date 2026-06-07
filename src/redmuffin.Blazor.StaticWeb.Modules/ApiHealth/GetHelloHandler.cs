using Mediator;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public sealed class GetHelloHandler : IRequestHandler<GetHelloQuery, HelloResponse>
{
    private readonly IHealthCheckService _healthCheckService;

    public GetHelloHandler(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async ValueTask<HelloResponse> Handle(GetHelloQuery request, CancellationToken cancellationToken)
    {
        var message = await _healthCheckService.GetHelloAsync(cancellationToken).ConfigureAwait(false);
        return new HelloResponse(message);
    }
}
