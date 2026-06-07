using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public sealed partial class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IHttpClientFactory httpClientFactory, ILogger<HealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(string.Empty);
            var response = await client.GetAsync("api/HelloWorld", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogFailedToCallHelloEndpoint(_logger, ex);
            throw;
        }
        catch (OperationCanceledException)
        {
            LogHelloEndpointCancelled(_logger);
            throw;
        }
    }
}
