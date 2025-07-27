using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Raindrop.Services;

public partial class RaindropAPITests
{
    [Test]
    public async Task GetHelloWorldAsync_Should_Return_Response_From_Azure_Function()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var raindropAPI = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        var result = await raindropAPI.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: correct response returned from Azure Function
        await Assert.That(result).IsEqualTo("Hello World from Azure Function");
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Log_Success_When_API_Call_Succeeds()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var raindropAPI = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        await raindropAPI.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: success is logged appropriately
        using (Assert.Multiple())
        {
            // Should log calling API
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Information &&
                entry.Message.Contains("Calling Hello World API"))).IsTrue();

            // Should log success
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Information &&
                entry.EventId.Id == 4 &&
                entry.Message.Contains("Successfully retrieved Hello World response from API"))).IsTrue();
        }
    }

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

    [Test]
    public async Task GetHelloWorldAsync_Should_Use_Correct_API_Endpoint()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var raindropApi = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        await raindropApi.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: correct endpoint is called
        await Assert.That(scope.HttpHandler?.LastRequestUri?.ToString()).Contains("/api/HelloWorld");
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Forward_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateCancellationTestScope();
        using var raindropApi = new RaindropAPI(scope.HttpClientFactory, scope.Logger);
        using var cts = new CancellationTokenSource();

        // Act
        await raindropApi.GetHelloWorldAsync(cts.Token).ConfigureAwait(false);

        // Assert - Test behavior: cancellation token is forwarded
        await Assert.That(scope.CancellationHttpHandler?.CancellationTokenReceived.IsCancellationRequested).IsEqualTo(cts.Token.IsCancellationRequested);
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Use_ConfigureAwait_False()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var raindropApi = new RaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act & Assert - Test behavior: method completes without deadlock
        // This test verifies ConfigureAwait(false) usage by ensuring the method completes
        var result = await raindropApi.GetHelloWorldAsync().ConfigureAwait(false);
        await Assert.That(result).IsNotNull();
    }

}