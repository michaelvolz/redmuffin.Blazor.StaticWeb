using System.Net;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Common.Services;
using redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ApiExamplePage;

public partial class CallApiExampleTests
{
    // Factory methods for creating test scopes - optimized for fast execution
    private static TestScope CreateTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithStandardServices();
    }

    private static TestScope CreateFailingHttpTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithFailingHttpClient();
    }

    private static TestScope CreateFailingServiceTestScope(string baseUri = "http://localhost:5000/")
    {
        return new TestScope(baseUri).WithFailingRaindropService();
    }

    /// <summary>
    ///     Modern test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    ///     Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
    {
        public BunitContext BUnitContext { get; } = new();
        public NavigationManagerMock NavigationManager { get; } = new(baseUri);
        public TestLogger<CallApiExample> Logger { get; } = new();
        public RaindropAPIMock RaindropAPIMock { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            // Configure the mock to return the expected response
            RaindropAPIMock.HelloWorldResponse = "Mock response";

            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<CallApiExample>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPIMock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a failing HTTP client for error testing scenarios.
        /// </summary>
        public TestScope WithFailingHttpClient()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<CallApiExample>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Failing);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPIMock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a failing Raindrop service for error testing scenarios.
        /// </summary>
        public TestScope WithFailingRaindropService()
        {
            var failingRaindropMock = new FailingRaindropAPIMock();
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<CallApiExample>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(failingRaindropMock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    // Mock NavigationManager for testing
    public class NavigationManagerMock : NavigationManager
    {
        public NavigationManagerMock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        public string? NavigatedTo { get; private set; }
        public bool NavigationCalled { get; private set; }
        public NavigationOptions? LastNavigationOptions { get; private set; }

        public void Reset()
        {
            NavigatedTo = null;
            NavigationCalled = false;
            LastNavigationOptions = null;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
            NavigationCalled = true;
            LastNavigationOptions = options;
        }
    }

    // Test logger to capture log messages
    public class TestLogger<T> : ILogger<T>
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

        public class LogEntry
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

    // Modern C# 12 HttpClient factory using primary constructor and static properties
    public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
    {
        public static TestHttpClientFactory Mock { get; } = new(() => new HttpMessageHandlerMock());
        public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandlerMock());

        public HttpClient CreateClient(string name = "")
        {
            var client = new HttpClient(handlerFactory(), true);
            client.BaseAddress = new Uri("http://localhost:5000/");
            return client;
        }
    }

    // Mock HttpMessageHandler that returns a successful response without making real network calls
    public sealed class HttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Handle relative URLs by checking the path
            if (request.RequestUri?.ToString().Contains("api/HelloWorld") == true ||
                request.RequestUri?.ToString().EndsWith("api/HelloWorld") == true)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Mock response", Encoding.UTF8, "text/plain")
                };
                return Task.FromResult(response);
            }

            // Default response for other requests
            var defaultResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Mock response", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(defaultResponse);
        }
    }

    public sealed class FailingHttpMessageHandlerMock : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network error");
        }
    }

    /// <summary>
    ///     Custom mock for IRaindropAPI following HomeTests patterns.
    ///     Tests behavior through public interface contracts, not implementation details.
    /// </summary>
    public class RaindropAPIMock : IRaindropAPI
    {
        public bool GetHelloWorldAsyncCalled { get; private set; }
        public bool GetVideosAsyncCalled { get; private set; }
        public bool GetArticlesAsyncCalled { get; private set; }
        public string HelloWorldResponse { get; set; } = "Hello World from Default Interface Implementation";

        public void Reset()
        {
            GetHelloWorldAsyncCalled = false;
            GetVideosAsyncCalled = false;
            GetArticlesAsyncCalled = false;
            HelloWorldResponse = "Hello World from Default Interface Implementation";
        }

        public Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default)
        {
            GetHelloWorldAsyncCalled = true;
            return Task.FromResult(HelloWorldResponse);
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            GetVideosAsyncCalled = true;
            return Task.FromResult(Enumerable.Empty<RaindropItem>());
        }

        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            GetArticlesAsyncCalled = true;
            return Task.FromResult(Enumerable.Empty<RaindropItem>());
        }
    }

    /// <summary>
    ///     Failing mock for IRaindropAPI to test error scenarios.
    /// </summary>
    public class FailingRaindropAPIMock : IRaindropAPI
    {
        public Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException("Simulated service error");
        }

        public Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException("Simulated service error");
        }

        public Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            throw new HttpRequestException("Simulated service error");
        }
    }
}