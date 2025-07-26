namespace redmuffin.Blazor.StaticWeb.Core.Services;

public class WarmupService(IHttpClientFactory httpClientFactory) : IWarmupService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task WarmupAsync()
    {
        try
        {
            // Fire-and-forget call to wake up the Azure Functions
            using var httpClient = _httpClientFactory.CreateClient("ExternalHttpClient");
            using var response = await httpClient.GetAsync("/api/HelloWorld").ConfigureAwait(false);
            // Intentionally not checking response - this is just to wake up the functions
        }
        catch
        {
            // Silent fail - we don't want warmup issues to break the app
        }
    }
}