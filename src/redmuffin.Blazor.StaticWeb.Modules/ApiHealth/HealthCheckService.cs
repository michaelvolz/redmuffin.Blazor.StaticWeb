using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

internal sealed partial class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IHttpClientFactory httpClientFactory, ILogger<HealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<string>> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _httpClientFactory.CreateClient(string.Empty);
            var response = await client.GetAsync("api/HelloWorld", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHelloEndpointNonSuccess(_logger);
                return Result.Failure<string>("The API endpoint returned an error response.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                LogHelloEndpointEmptyResponse(_logger);
                return Result.Failure<string>("The API endpoint returned an empty response.");
            }

            return Result.Success(body);
        }
        catch (HttpRequestException ex)
        {
            LogFailedToCallHelloEndpoint(_logger, ex);
            return Result.Failure<string>("The API endpoint did not return a response.");
        }
        catch (OperationCanceledException)
        {
            LogHelloEndpointCancelled(_logger);
            throw;
        }
    }
}
