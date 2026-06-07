using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class GetHelloHandlerTests
{
    [Test]
    public async Task Returns_hello_message_when_service_returns_data()
    {
        using var scope = CreateScope("Expected response");
        var query = new GetHelloQuery();

        var result = await scope.Handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result.Message).IsEqualTo("Expected response");
    }

    [Test]
    public async Task Throws_when_service_fails()
    {
        using var scope = CreateScope(string.Empty);
        scope.HealthCheckService.Exception = new HttpRequestException("Service error");
        var query = new GetHelloQuery();

        var act = async () => await scope.Handler.Handle(query, CancellationToken.None).ConfigureAwait(false);
        await Assert.That(act).Throws<HttpRequestException>();
    }
}
