namespace redmuffin.Blazor.StaticWeb.Core.Services;

public class WarmupService(IHttpClientFactory httpClientFactory) : IWarmupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task<bool> TryWarmupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync("/api/HelloWorld", cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }
}
