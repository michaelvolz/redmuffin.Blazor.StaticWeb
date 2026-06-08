using System.Net;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class HealthCheckServiceTests
{
    [Test]
    public async Task Throws_and_logs_when_connection_fails()
    {
        // Arrange
        using var scope = CreateScope(
            _ => throw new HttpRequestException("Connection refused"));

        // Act
        var act = async () => await scope.Service.GetHelloAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(act).Throws<HttpRequestException>();
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Error);
        await Assert.That(scope.LogEntries[0].Exception).IsNotNull();
    }

    [Test]
    [Arguments(HttpStatusCode.NotFound)]
    [Arguments(HttpStatusCode.InternalServerError)]
    [Arguments(HttpStatusCode.ServiceUnavailable)]
    public async Task Throws_and_logs_when_server_returns_non_2xx(HttpStatusCode statusCode)
    {
        // Arrange
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(statusCode)));

        // Act
        var act = async () => await scope.Service.GetHelloAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(act).Throws<HttpRequestException>();
        await AssertThatSingleWarningLogged(scope).ConfigureAwait(false);
    }

    [Test]
    public async Task Throws_and_logs_when_request_is_cancelled()
    {
        // Arrange
        var cancellationToken = new CancellationToken(canceled: true);
        using var scope = CreateScope(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act
        var act = async () => await scope.Service.GetHelloAsync(cancellationToken).ConfigureAwait(false);

        // Assert
        await Assert.That(act).Throws<OperationCanceledException>();
        await AssertThatSingleWarningLogged(scope).ConfigureAwait(false);
    }

    [Test]
    public async Task Throws_and_logs_when_request_times_out()
    {
        // Arrange
        using var scope = CreateScope(
            _ => throw new TaskCanceledException("The request timed out"));

        // Act
        var act = async () => await scope.Service.GetHelloAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(act).Throws<TaskCanceledException>();
        await AssertThatSingleWarningLogged(scope).ConfigureAwait(false);
    }

    private static async Task AssertThatSingleWarningLogged(TestScope scope)
    {
        await Assert.That(scope.LogEntries.Count).IsEqualTo(1);
        await Assert.That(scope.LogEntries[0].Level).IsEqualTo(LogLevel.Warning);
    }
}
