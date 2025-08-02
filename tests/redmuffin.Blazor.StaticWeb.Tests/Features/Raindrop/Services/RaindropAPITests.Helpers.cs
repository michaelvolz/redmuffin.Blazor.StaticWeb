using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

public partial class RaindropAPITests
{
    // Factory methods for creating test scopes - optimized for fast execution
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    private static TestScope CreateFailingHttpTestScope()
    {
        return new TestScope().WithFailingHttpClient();
    }

    private static TestScope CreateCancellationTestScope()
    {
        return new TestScope().WithCancellationAwareHttpClient();
    }

    /// <summary>
    ///     Modern test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public HttpClientFactory_Stub HttpClientFactory { get; private set; } = HttpClientFactory_Stub.Mock;
        public Logger_Spy<RaindropAPI> Logger { get; } = new();
        public HttpMessageHandler_Mock? HttpHandler { get; private set; }
        public HttpMessageHandler_FailingMock? FailingHttpHandler { get; private set; }
        public HttpMessageHandler_CancellationAwareMock? CancellationHttpHandler { get; private set; }

        /// <summary>
        ///     Configures the test scope with standard HTTP client for successful API calls.
        /// </summary>
        public TestScope WithStandardServices()
        {
            HttpHandler = new HttpMessageHandler_Mock("Hello World from Azure Function");
            HttpClientFactory = new HttpClientFactory_Stub(() => HttpHandler);
            return this;
        }

        /// <summary>
        ///     Configures the test scope with a failing HTTP client for error testing scenarios.
        /// </summary>
        public TestScope WithFailingHttpClient()
        {
            FailingHttpHandler = new HttpMessageHandler_FailingMock();
            HttpClientFactory = new HttpClientFactory_Stub(() => FailingHttpHandler);
            return this;
        }

        /// <summary>
        ///     Configures the test scope with a cancellation-aware HTTP client for cancellation token testing.
        /// </summary>
        public TestScope WithCancellationAwareHttpClient()
        {
            CancellationHttpHandler = new HttpMessageHandler_CancellationAwareMock();
            HttpClientFactory = new HttpClientFactory_Stub(() => CancellationHttpHandler);
            return this;
        }

        public void Dispose()
        {
            HttpHandler?.Dispose();
            FailingHttpHandler?.Dispose();
            CancellationHttpHandler?.Dispose();
        }
    }

    // Modern C# 12 HttpClient factory using primary constructor and static properties
    public sealed class HttpClientFactory_Stub(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
    {
        public static HttpClientFactory_Stub Mock { get; } = new(() => new HttpMessageHandler_Mock("Hello World from Azure Function"));
        public static HttpClientFactory_Stub Failing { get; } = new(() => new HttpMessageHandler_FailingMock());

        public HttpClient CreateClient(string name = "")
        {
            var client = new HttpClient(handlerFactory(), true);
            client.BaseAddress = new Uri("https://localhost:5000/");
            return client;
        }
    }

    // Mock HTTP message handler that returns a successful response
    public sealed class HttpMessageHandler_Mock(string responseContent) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(response);
        }
    }

    // Mock HTTP message handler that throws exceptions
    public sealed class HttpMessageHandler_FailingMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network error");
        }
    }

    // Mock HTTP message handler that captures cancellation token
    public sealed class HttpMessageHandler_CancellationAwareMock : HttpMessageHandler
    {
        public CancellationToken CancellationTokenReceived { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CancellationTokenReceived = cancellationToken;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello World from Azure Function", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(response);
        }
    }

    // Test logger implementation for capturing log messages
    public sealed class Logger_Spy<T> : ILogger<T>
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
}
