using Mediator;
using Microsoft.AspNetCore.Components;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.ApiHealth;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class ApiHealth
#pragma warning restore MA0049
{
    private string? _apiResponse;
    private string? _errorMessage;

    [Inject]
    private IMediator Mediator { get; set; } = default!; // Injected by DI container

    protected override void OnInitialized()
    {
#pragma warning disable MA0015 // Not a method parameter — validating Blazor [Inject] property
        ArgumentNullException.ThrowIfNull(Mediator);
#pragma warning restore MA0015
        base.OnInitialized();
    }

    private async Task CallApiAsync()
    {
        _apiResponse = null;
        _errorMessage = null;

        try
        {
            var response = await Mediator.Send(new GetHelloQuery()).ConfigureAwait(false);
            _apiResponse = response.Message;
        }
        catch (HttpRequestException ex)
        {
            _errorMessage = $"Error calling API: {ex.Message}";
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Unexpected error: {ex.Message}";
            Console.WriteLine(ex);
        }
    }
}
