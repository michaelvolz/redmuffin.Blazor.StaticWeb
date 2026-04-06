using redmuffin.Blazor.StaticWeb.Configuration;

namespace redmuffin.Blazor.StaticWeb.Tests.Configuration;

/// <summary>
///     TUnit tests for PageLoadSpeedConfig.
/// </summary>
[Category("Feature:Configuration")]
[Category("Unit")]
public sealed partial class PageLoadSpeedConfigTests
{
    [Test]
    public async Task ShouldDisplayComponent_Should_Return_True_When_Localhost_Is_Allowed()
    {
        // Arrange
        using var scope = await EnterExclusiveScopeAsync().ConfigureAwait(false);
        PageLoadSpeedConfig.IsEnabled = true;
        PageLoadSpeedConfig.EnableOnLocalhost = true;

        // Act
        var localhostResult = PageLoadSpeedConfig.ShouldDisplayComponent("http://localhost:5000/");
        var productionResult = PageLoadSpeedConfig.ShouldDisplayComponent("https://example.com/");

        // Assert
        await Assert.That(localhostResult).IsTrue();
        await Assert.That(productionResult).IsTrue();
    }

    [Test]
    public async Task ShouldDisplayComponent_Should_Hide_Local_Hosts_When_Localhost_Is_Disallowed()
    {
        // Arrange
        using var scope = await EnterExclusiveScopeAsync().ConfigureAwait(false);
        PageLoadSpeedConfig.IsEnabled = true;
        PageLoadSpeedConfig.EnableOnLocalhost = false;

        // Act
        var localhostResult = PageLoadSpeedConfig.ShouldDisplayComponent("http://localhost:5000/");
        var loopbackResult = PageLoadSpeedConfig.ShouldDisplayComponent("http://127.0.0.1:5000/");
        var privateNetworkResult = PageLoadSpeedConfig.ShouldDisplayComponent("http://10.0.0.5/");
        var productionResult = PageLoadSpeedConfig.ShouldDisplayComponent("https://example.com/");

        // Assert
        await Assert.That(localhostResult).IsFalse();
        await Assert.That(loopbackResult).IsFalse();
        await Assert.That(privateNetworkResult).IsFalse();
        await Assert.That(productionResult).IsTrue();
    }

    [Test]
    public async Task ShouldDisplayComponent_Should_Return_False_When_Component_Is_Disabled()
    {
        // Arrange
        using var scope = await EnterExclusiveScopeAsync().ConfigureAwait(false);
        PageLoadSpeedConfig.IsEnabled = false;
        PageLoadSpeedConfig.EnableOnLocalhost = true;

        // Act
        var result = PageLoadSpeedConfig.ShouldDisplayComponent("https://example.com/");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ShouldDisplayComponent_Should_Throw_When_BaseUri_Is_Null_And_Localhost_Is_Disallowed()
    {
        // Arrange
        using var scope = await EnterExclusiveScopeAsync().ConfigureAwait(false);
        PageLoadSpeedConfig.IsEnabled = true;
        PageLoadSpeedConfig.EnableOnLocalhost = false;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(PageLoadSpeedConfig.ShouldDisplayComponent(null!)));
    }
}
