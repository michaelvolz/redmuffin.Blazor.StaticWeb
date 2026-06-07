using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class SyntheticHealthCheckServiceTests
{
    [Test]
    public async Task Returns_expected_synthetic_string()
    {
        // Arrange
        var service = new SyntheticHealthCheckService();

        // Act
        var result = await service.GetHelloAsync().ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsEqualTo("Hello from the ApiHealth module! (synthetic data)");
    }
}
