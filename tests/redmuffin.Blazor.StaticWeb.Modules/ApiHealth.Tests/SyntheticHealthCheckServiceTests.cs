namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed class SyntheticHealthCheckServiceTests
{
    [Test]
    public async Task Returns_expected_synthetic_string()
    {
        var service = new SyntheticHealthCheckService();

        var result = await service.GetHelloAsync().ConfigureAwait(false);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Hello from the ApiHealth module! (synthetic data)");
    }
}
