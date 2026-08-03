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
using HomePage = redmuffin.Blazor.StaticWeb.Features.HomePage.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
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
            ? new ClaimsIdentity_Mock(userName ?? "testuser@example.com", "mock")
            : new ClaimsIdentity_Mock();

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
        public NavigationManager_Mock NavigationManager { get; } = new(baseUri);
        public Logger_Spy<HomePage> Logger { get; } = new();

        /// <summary>
        ///     Configures the test context with high-performance services for optimal test execution.
        /// </summary>
        public TestScope WithStandardServices()
        {
            BUnitContext.Services.AddSingleton<NavigationManager>(NavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(PageAssemblyLoader_Stub.Instance);
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
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Failing);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(PageAssemblyLoader_Stub.Instance);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with a faulty navigation manager for error handling tests.
        /// </summary>
        public TestScope WithFaultyNavigation()
        {
            var faultyNavigationManager = new NavigationManager_FaultyMock(baseUri);
            BUnitContext.Services.AddSingleton<NavigationManager>(faultyNavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(PageAssemblyLoader_Stub.Instance);
            BUnitContext.JSInterop.Mode = JSRuntimeMode.Loose;
            return this;
        }

        /// <summary>
        ///     Configures the test context with an exception-throwing navigation manager for error handling tests.
        /// </summary>
        public TestScope WithThrowingNavigation()
        {
            var throwingNavigationManager = new NavigationManager_ThrowingMock(baseUri);
            BUnitContext.Services.AddSingleton<NavigationManager>(throwingNavigationManager);
            BUnitContext.Services.AddSingleton<ILogger<HomePage>>(Logger);
            BUnitContext.Services.AddSingleton<IHttpClientFactory>(HttpClientFactory_Stub.Mock);
            BUnitContext.Services.AddSingleton<IDelayProvider>(new DelayProvider_Stub()); // ✅ FAST: No delays in tests
            BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(PageAssemblyLoader_Stub.Instance);
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

            // Debug logging
            Console.WriteLine("NavigationManager_Mock.NavigateToCore called:");
            Console.WriteLine($"  - URI: {uri}");
            Console.WriteLine($"  - Options: ForceLoad={options.ForceLoad}, ReplaceHistoryEntry={options.ReplaceHistoryEntry}");
        }
    }

    // Mock NavigationManager that throws exceptions to test error handling
    public class NavigationManager_ThrowingMock : NavigationManager
    {
        public NavigationManager_ThrowingMock(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            throw new InvalidOperationException("Navigation failed due to simulated error");
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
        public static HttpClientFactory_Stub Failing { get; } = new(() => new FailingHttpMessageHandler());

        public HttpClient CreateClient(string name = "")
        {
            return new HttpClient(handlerFactory(), true);
        }
    }

    // Mock HttpMessageHandler that returns a successful response without making real network calls
    public sealed class HttpMessageHandler_Mock : HttpMessageHandler
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

    public class NavigationManager_FaultyMock : NavigationManager
    {
        public NavigationManager_FaultyMock(string baseUri)
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

            Console.WriteLine("NavigationManager_FaultyMock.NavigateToCore called:");
            Console.WriteLine($"  - URI: {uri}");
            Console.WriteLine("  - Navigation failed silently (simulated fault)");
        }
    }

    /// <summary>
    ///     Stub implementation of IDelayProvider that provides no delays for fast test execution.
    /// </summary>
    /// <summary>
    ///     No-op page assembly loader for Home bUnit tests (dormant catalog path).
    /// </summary>
    public sealed class PageAssemblyLoader_Stub : IPageAssemblyLoader
    {
        public static PageAssemblyLoader_Stub Instance { get; } = new();

        public IReadOnlyList<System.Reflection.Assembly> LoadedAssemblies { get; } = [];

        public Task EnsureLoadedAsync(string pageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PrefetchHomePrimaryJourneysAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

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
    ///     Mock ClaimsIdentity for testing authorization scenarios.
    /// </summary>
    public sealed class ClaimsIdentity_Mock(string? name = null, string? authenticationType = null) : ClaimsIdentity(CreateClaims(name), authenticationType)
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