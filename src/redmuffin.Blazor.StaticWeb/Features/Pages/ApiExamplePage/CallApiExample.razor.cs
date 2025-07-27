using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;

public partial class CallApiExample
{
    private string? _apiResponse;
    private string? _errorMessage;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = default!;

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(RaindropAPI);
        base.OnInitialized();
    }

    private async Task CallApiAsync()
    {
        ArgumentNullException.ThrowIfNull(RaindropAPI);

        _apiResponse = null;
        _errorMessage = null;
        try
        {
            _apiResponse = await RaindropAPI.GetHelloWorldAsync().ConfigureAwait(false);
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