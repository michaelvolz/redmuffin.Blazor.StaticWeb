using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

public class DummyRaindropAPITests
{
    // Factory method for creating test scopes - optimized for fast execution
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    /// <summary>
    ///     Test scope that encapsulates all test resources with automatic disposal.
    ///     Uses the same pattern as HomeTests for consistency.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestHttpClientFactory HttpClientFactory { get; } = TestHttpClientFactory.Mock;
        public TestLogger<DummyRaindropAPI> Logger { get; } = new();

        public void Dispose()
        {
            // No explicit disposal needed for these test objects
        }
    }

    // Modern C# 12 HttpClient factory using primary constructor and static properties
    public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
    {
        public static TestHttpClientFactory Mock { get; } = new(() => new HttpMessageHandlerMock());
        public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandler());

        public HttpClient CreateClient(string name = "")
        {
            return new HttpClient(handlerFactory(), true);
        }
    }

    // Mock HttpMessageHandler that returns a successful response without making real network calls
    public sealed class HttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Mock response", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    public sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network error");
        }
    }

    // Test logger implementation for capturing log messages
    public sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoOpDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception
            });
        }

        public sealed class LogEntry
        {
            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public string Message { get; set; } = string.Empty;
            public Exception? Exception { get; set; }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Be_Consistent_Across_Multiple_Calls()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act - Call multiple times
        var result1 = await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);
        var result2 = await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);
        var result3 = await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: consistent responses
        using (Assert.Multiple())
        {
            await Assert.That(result1).IsEqualTo(result2);
            await Assert.That(result2).IsEqualTo(result3);
            await Assert.That(result1).IsEqualTo("Hello World from Mock Data - Not from Azure Functions");
        }
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Complete_Synchronously()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act & Assert - Test behavior: method completes without delay
        var task = dummyApi.GetHelloWorldAsync();
        await Assert.That(task.IsCompleted).IsTrue(); // Should be completed immediately

        var result = await task.ConfigureAwait(false);
        await Assert.That(result).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false); // Pre-cancel the token

        // Act & Assert - Test behavior: method respects cancellation
        var result = await dummyApi.GetHelloWorldAsync(cts.Token).ConfigureAwait(false);

        // Since this is a synchronous mock, it should still return the result
        // even with a cancelled token (this is expected behavior for Task.FromResult)
        await Assert.That(result).IsEqualTo("Hello World from Mock Data - Not from Azure Functions");
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Log_Each_Call_Separately()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act - Make multiple calls
        await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);
        await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: each call is logged
        var helloWorldLogEntries = scope.Logger.LogEntries.Where(entry =>
            entry.LogLevel == LogLevel.Information &&
            entry.EventId.Id == 4 &&
            entry.Message.Contains("Hello World mock response")).ToList();

        await Assert.That(helloWorldLogEntries.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Log_Mock_Response_Information()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: appropriate logging occurs
        using (Assert.Multiple())
        {
            await Assert.That(scope.Logger.LogEntries.Count).IsGreaterThan(0);
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Information &&
                entry.EventId.Id == 4 &&
                entry.Message.Contains("Hello World mock response"))).IsTrue();
        }
    }

    [Test]
    public async Task GetHelloWorldAsync_Should_Return_Expected_Mock_Response()
    {
        // Arrange
        using var scope = CreateTestScope();
        using var dummyApi = new DummyRaindropAPI(scope.HttpClientFactory, scope.Logger);

        // Act
        var result = await dummyApi.GetHelloWorldAsync().ConfigureAwait(false);

        // Assert - Test behavior: correct response returned
        await Assert.That(result).IsEqualTo("Hello World from Mock Data - Not from Azure Functions");
    }
}