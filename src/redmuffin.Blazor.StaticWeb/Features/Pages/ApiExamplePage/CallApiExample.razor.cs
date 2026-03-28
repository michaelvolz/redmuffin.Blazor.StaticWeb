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
#pragma warning disable MA0015 // Not a method parameter — validating Blazor [Inject] property
        ArgumentNullException.ThrowIfNull(RaindropAPI);
#pragma warning restore MA0015
        base.OnInitialized();
    }

    private async Task CallApiAsync()
    {
#pragma warning disable MA0015 // Not a method parameter — validating Blazor [Inject] property
        ArgumentNullException.ThrowIfNull(RaindropAPI);
#pragma warning restore MA0015

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