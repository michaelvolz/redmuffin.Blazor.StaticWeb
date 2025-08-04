using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

public sealed partial class RaindropAPITests
{
    [Test]
    public async Task GetHelloWorldAsync_Should_Handle_HttpRequestException_Gracefully()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        using var raindropAPI = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act & Assert - Test behavior: HTTP exceptions are handled gracefully
        await Assert.ThrowsAsync<HttpRequestException>(() => raindropAPI.GetHelloWorldAsync());
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Log_Error_When_API_Call_Fails()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        using var raindropApi = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        try
        {
            await raindropApi.GetHelloWorldAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Expected exception
        }

        // Assert - Test behavior: error is logged appropriately
        using (Assert.Multiple())
        {
            // Should log calling API
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Information &&
                entry.Message.Contains("Calling Hello World API"))).IsTrue();

            // Should log error
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Error &&
                entry.EventId.Id == 5 &&
                entry.Message.Contains("Hello World API request failed"))).IsTrue();
        }
    }
}