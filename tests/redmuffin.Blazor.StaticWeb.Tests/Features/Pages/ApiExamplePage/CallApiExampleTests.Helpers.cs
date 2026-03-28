using System.Net;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Pages.ApiExamplePage;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ApiExamplePage;

[Category("Feature:ApiExample")]
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
        public NavigationManager_Mock NavigationManager { get; } = new(baseUri);
        public Logger_Spy<CallApiExample> Logger { get; } = new();
        public RaindropAPI_Mock RaindropAPI_Mock { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            // Configure the mock to return the expected response
            RaindropAPI_Mock.HelloWorldResponse = "Mock response";

            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<CallApiExample>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Mock);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPI_Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
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
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Failing);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(RaindropAPI_Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a failing Raindrop service for error testing scenarios.
        /// </summary>
        public TestScope WithFailingRaindropService()
        {
            var raindropAPI_FailingMock = new RaindropAPI_FailingMock();
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<CallApiExample>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Mock);
            BUnitContext.Services.AddSingleton<IRaindropAPI>(raindropAPI_FailingMock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        public void Dispose()
        {
            BUnitContext?.Dispose();
        }
    }

    // Mock NavigationManager for testing
    public class NavigationManager_Mock : NavigationManager
    {
        public NavigationManager_Mock(string baseUri)
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
    public class Logger_Spy<T> : ILogger<T>
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
    public sealed class HttpClientFactory_Stub(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
    {
        public static HttpClientFactory_Stub Mock { get; } = new(() => new HttpMessageHandler_Mock());
        public static HttpClientFactory_Stub Failing { get; } = new(() => new HttpMessageHandler_FailingMock());

        public HttpClient CreateClient(string name = "")
        {
            var client = new HttpClient(handlerFactory(), true);
            client.BaseAddress = new Uri("http://localhost:5000/");
            return client;
        }
    }

    // Mock HttpMessageHandler that returns a successful response without making real network calls
    public sealed class HttpMessageHandler_Mock : HttpMessageHandler
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

    public sealed class HttpMessageHandler_FailingMock : HttpMessageHandler
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
    public class RaindropAPI_Mock : IRaindropAPI
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
    ///     Stub implementation of IDelayProvider that provides no delays for fast test execution.
    /// </summary>
    public sealed class DelayProvider_Stub : IDelayProvider
    {
        /// <inheritdoc />
        public Task DelayAsync(int milliseconds)
        {
            // No delay in test scenarios for optimal performance
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Failing mock for IRaindropAPI to test error scenarios.
    /// </summary>
    public class RaindropAPI_FailingMock : IRaindropAPI
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