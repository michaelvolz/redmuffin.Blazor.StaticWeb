namespace redmuffin.Blazor.StaticWeb.Core.Services;

public class WarmupService(HttpClient httpClient) : IWarmupService
{
    public async Task WarmupAsync()
    {
        try
        {
            // Fire-and-forget call to wake up the Azure Functions
            using var response = await httpClient.GetAsync("/api/HelloWorld").ConfigureAwait(false);
            // Intentionally not checking response - this is just to wake up the functions
        }
        catch
        {
            // Silent fail - we don't want warmup issues to break the app
        }
    }
}