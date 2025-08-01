using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;
using redmuffin.Blazor.StaticWeb.Core.Services;
using HomePage = redmuffin.Blazor.StaticWeb.Features.Pages.HomePage.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

public partial class HomeTests
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

    // ========================================
    // HELPER METHODS FOR AUTHORIZATION TESTING
    // ========================================

    /// <summary>
    ///     Creates a mock AuthenticationState for testing authorization scenarios.
    /// </summary>
    /// <param name="isAuthenticated">Whether the user should be authenticated.</param>
    /// <param name="userName">The username for authenticated users.</param>
    /// <returns>A Task containing the mock AuthenticationState.</returns>
    private static Task<AuthenticationState> CreateMockAuthenticationState(bool isAuthenticated, string? userName = null)
    {
        var identity = isAuthenticated
            ? new MockClaimsIdentity(userName ?? "testuser@example.com", "mock")
            : new MockClaimsIdentity();

        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        return Task.FromResult(authState);
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
        public TestLogger<HomePage> Logger { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
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
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Failing);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a faulty navigation manager for error handling tests.
        /// </summary>
        public TestScope WithFaultyNavigation()
        {
            var faultyNavigationManager = new FaultyNavigationManagerMock(baseUri);
            BUnitContext.Services.AddSingleton<NavigationManager>(faultyNavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with an exception-throwing navigation manager for error handling tests.
        /// </summary>
        public TestScope WithThrowingNavigation()
        {
            var throwingNavigationManager = new ThrowingNavigationManagerMock(baseUri);
            BUnitContext.Services.AddSingleton<NavigationManager>(throwingNavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a fast timeout HTTP client for testing async operation timeouts.
        /// </summary>
        public TestScope WithTimeoutHttpClient()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.FastTimeout);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new TestDelayProvider()); // ✅ FAST: No delays in tests
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures JS interop mode for testing JavaScript integration scenarios.
        /// </summary>
        public TestScope WithJSInterop(JSRuntimeMode mode = JSRuntimeMode.Strict)
        {
            BUnitContext.JSInterop.Mode = mode;
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

            // Debug logging
            Console.WriteLine("NavigationManagerMock.NavigateToCore called:");
            Console.WriteLine($"  - URI: {uri}");
            Console.WriteLine($"  - Options: ForceLoad={options.ForceLoad}, ReplaceHistoryEntry={options.ReplaceHistoryEntry}");
        }
    }

    // Mock NavigationManager that throws exceptions to test error handling
    public class ThrowingNavigationManagerMock : NavigationManager
    {
        public ThrowingNavigationManagerMock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            throw new InvalidOperationException("Navigation failed due to simulated error");
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
        public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandler());
        public static TestHttpClientFactory FastTimeout { get; } = new(() => new FastTimeoutHttpMessageHandler()); // ✅ FAST: 100ms timeout

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

    // ✅ OPTIMIZED: Fast timeout handler (100ms instead of 60 seconds)
    public sealed class FastTimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ✅ FAST: Use 100ms timeout instead of 1 minute for test performance
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            throw new TaskCanceledException("Request timed out");
        }
    }

    public class FaultyNavigationManagerMock : NavigationManager
    {
        public FaultyNavigationManagerMock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        public string? NavigatedTo { get; private set; }
        public bool NavigationCalled { get; private set; }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            // Log the navigation attempt but don't throw to allow component to render
            // This simulates a navigation that fails silently
            NavigatedTo = uri;
            NavigationCalled = true;

            Console.WriteLine("FaultyNavigationManagerMock.NavigateToCore called:");
            Console.WriteLine($"  - URI: {uri}");
            Console.WriteLine("  - Navigation failed silently (simulated fault)");
        }
    }

    /// <summary>
    ///     Mock ClaimsIdentity for testing authorization scenarios.
    /// </summary>
    public sealed class MockClaimsIdentity(string? name = null, string? authenticationType = null) : ClaimsIdentity(CreateClaims(name), authenticationType)
    {
        public override bool IsAuthenticated => !string.IsNullOrEmpty(AuthenticationType);

        private static IEnumerable<Claim> CreateClaims(string? name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                yield return new Claim(ClaimTypes.Name, name);
                yield return new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString());
            }
        }
    }
}