using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;
using redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Contracts;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ApiHealth;

[Category("Feature:ApiHealth")]
public sealed partial class GetHelloHandlerTests
{
    [Test]
    public async Task Returns_success_when_service_returns_data()
    {
        using var scope = CreateScope("Expected response");
        var query = new GetHelloQuery();

        var result = await scope.Handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Message).IsEqualTo("Expected response");
    }

    [Test]
    public async Task Returns_failure_when_service_fails()
    {
        using var scope = CreateScope(string.Empty);
        scope.HealthCheckService.FailureError = "Service error";
        var query = new GetHelloQuery();

        var result = await scope.Handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Service error");
    }
}
