using System.Net;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class HealthCheckServiceTests
{
    [Test]
    public async Task Returns_success_when_endpoint_responds_with_body()
    {
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello from API")
            }));

        var result = await scope.Service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Hello from API");
        await Assert.That(scope.LogEntries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Returns_failure_and_logs_when_connection_fails()
    {
        using var scope = CreateScope(
            _ => throw new HttpRequestException("Connection refused"));

        var result = await scope.Service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("The API endpoint did not return a response.");
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Error);
        await Assert.That(scope.LogEntries[0].Exception).IsNotNull();
    }

    [Test]
    [Arguments(HttpStatusCode.NotFound)]
    [Arguments(HttpStatusCode.InternalServerError)]
    [Arguments(HttpStatusCode.ServiceUnavailable)]
    public async Task Returns_failure_and_logs_when_server_returns_non_2xx(HttpStatusCode statusCode)
    {
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(statusCode)));

        var result = await scope.Service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("The API endpoint returned an error response.");
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Error);
    }

    [Test]
    public async Task Returns_failure_and_logs_when_response_body_is_empty()
    {
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            }));

        var result = await scope.Service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("The API endpoint returned an empty response.");
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Error);
    }

    [Test]
    public async Task Throws_and_logs_when_request_is_cancelled()
    {
        var cancellationToken = new CancellationToken(canceled: true);
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            }));

        var act = async () => await scope.Service.GetHelloAsync(cancellationToken).ConfigureAwait(false);

        await Assert.That(act).Throws<OperationCanceledException>();
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Warning);
    }

    [Test]
    public async Task Throws_and_logs_when_request_times_out()
    {
        using var scope = CreateScope(
            _ => throw new TaskCanceledException("The request timed out"));

        var act = async () => await scope.Service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(act).Throws<TaskCanceledException>();
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Warning);
    }
}
