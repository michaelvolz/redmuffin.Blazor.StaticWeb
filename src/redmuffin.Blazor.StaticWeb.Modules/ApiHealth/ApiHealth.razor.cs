using System.Diagnostics;
using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class ApiHealth
#pragma warning restore MA0049
{
    private readonly IMediator _mediator;
    private readonly Stopwatch _stopwatch = new();

    private ApiHealthViewModel _viewModel = ApiHealthViewModel.Idle;

    public ApiHealth(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    private async Task RunHealthCheckAsync()
    {
        _viewModel = ApiHealthViewModel.Loading();
        _stopwatch.Restart();

        try
        {
            var result = await _mediator.Send(new GetHelloQuery()).ConfigureAwait(false);
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed.TotalMilliseconds;

            _viewModel = result.Match(
                response =>
                {
                    var data = new ApiHealthData(
                        response.Message,
                        FormattableString.Invariant($"{elapsed:F1} ms"),
                        BuildHealthChecks(response, elapsed));
                    return ApiHealthViewModel.Healthy(data);
                },
                error => ApiHealthViewModel.Unhealthy(error));
        }
        catch (OperationCanceledException)
        {
            _viewModel = ApiHealthViewModel.Unhealthy("The health check was cancelled.");
        }
        catch (Exception ex)
        {
            _viewModel = ApiHealthViewModel.Unhealthy($"Unexpected error: {ex.Message}");
        }
    }

    private static IReadOnlyList<HealthCheckItem> BuildHealthChecks(HelloResponse response, double elapsedMs)
    {
        var messageValid = response.Message.Length > 0;
        var preview = messageValid
            ? $"\"{response.Message[..Math.Min(response.Message.Length, 30)]}\""
            : "Empty response";
        var latencyOk = elapsedMs < 500;
        var latencyValue = latencyOk
            ? FormattableString.Invariant($"{elapsedMs:F1} ms")
            : FormattableString.Invariant($"{elapsedMs:F1} ms (exceeds 500ms threshold)");

        return
        [
            new HealthCheckItem("Message Valid", preview, messageValid),
            new HealthCheckItem("Latency", latencyValue, latencyOk)
        ];
    }
}
