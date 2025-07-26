using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;

public partial class CallApiExample
{
    private string? _apiResponse;
    private string? _errorMessage;

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private async Task CallApiAsync()
    {
        ArgumentNullException.ThrowIfNull(HttpClientFactory);

        _apiResponse = null;
        _errorMessage = null;
        try
        {
            using var httpClient = HttpClientFactory.CreateClient("DefaultClient");
            // The API endpoint is relative to the application's base URI
            _apiResponse = await httpClient.GetStringAsync("api/HelloWorld").ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _errorMessage = $"Error calling API: {ex.Message}";
            // Log the full exception if needed for debugging
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Unexpected error: {ex.Message}";
            Console.WriteLine(ex);
        }
    }
}