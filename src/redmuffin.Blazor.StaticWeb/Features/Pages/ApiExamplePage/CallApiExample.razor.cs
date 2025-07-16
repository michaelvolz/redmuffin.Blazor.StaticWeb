using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;

public partial class CallApiExample
{
    private string? _apiResponse;
    private string? _errorMessage;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    private async Task CallApiAsync()
    {
        _apiResponse = null;
        _errorMessage = null;
        try
        {
            // The API endpoint is relative to the application's base URI
            _apiResponse = await Http.GetStringAsync("api/HelloWorld").ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _errorMessage = $"Error calling API: {ex.Message}";
            // Log the full exception if needed for debugging
            Console.WriteLine(ex);
        }
    }
}