using System.Net;
using System.Text;
using System.Text.Json;
using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Raindrop.Services;

public partial class IRaindropAPITests
{
    // Factory methods for creating test scopes
    private static TestScope CreateDummyAPITestScope()
    {
        return new TestScope().WithDummyAPI();
    }

    private static TestScope CreateDummyAPITestScopeWithMissingFiles()
    {
        return new TestScope().WithDummyAPIMissingFiles();
    }

    private static TestScope CreateRealAPITestScope()
    {
        return new TestScope().WithRealAPI();
    }

    private static TestScope CreateFailingAPITestScope()
    {
        return new TestScope().WithFailingRealAPI();
    }

    private static TestScope CreateMalformedResponseAPITestScope()
    {
        return new TestScope().WithMalformedResponseAPI();
    }

    /// <summary>
    /// Modern test scope that encapsulates all test resources with automatic disposal.
    /// Uses C# 13 primary constructor pattern for clean, professional resource management.
    /// Optimized for fast test execution with zero-delay providers.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestLogger<DummyRaindropAPI> DummyLogger { get; } = new();
    public TestLogger<RaindropAPI> RealLogger { get; } = new();
        public DummyRaindropAPI? DummyAPI { get; private set; }
        public RaindropAPI? RealAPI { get; private set; }
        public ILogger Logger => DummyAPI != null ? DummyLogger : RealLogger;
        public TestLogger<DummyRaindropAPI> GetDummyLogger() => DummyLogger;
    public TestLogger<RaindropAPI> GetRealLogger() => RealLogger;

        /// <summary>
        /// Configures the test scope with a DummyRaindropAPI instance that loads data from mock JSON files.
        /// </summary>
        public TestScope WithDummyAPI()
        {
            DummyAPI = new DummyRaindropAPI(TestHttpClientFactory.Mock, DummyLogger);
            ArgumentNullException.ThrowIfNull(DummyAPI);
            return this;
        }

        /// <summary>
        /// Configures the test scope with a DummyRaindropAPI instance that simulates missing files.
        /// </summary>
        public TestScope WithDummyAPIMissingFiles()
        {
            DummyAPI = new DummyRaindropAPI(TestHttpClientFactory.MissingFiles, DummyLogger);
            ArgumentNullException.ThrowIfNull(DummyAPI);
            return this;
        }

        /// <summary>
        /// Configures the test scope with a RaindropAPI instance that returns successful responses.
        /// </summary>
        public TestScope WithRealAPI()
        {
            RealAPI = new RaindropAPI(TestHttpClientFactory.RealAPI, RealLogger);
            return this;
        }

        /// <summary>
        /// Configures the test scope with a RaindropAPI instance that simulates API failures.
        /// </summary>
        public TestScope WithFailingRealAPI()
        {
            RealAPI = new RaindropAPI(TestHttpClientFactory.Failing, RealLogger);
            return this;
        }

        /// <summary>
        /// Configures the test scope with a RaindropAPI instance that returns malformed responses.
        /// </summary>
        public TestScope WithMalformedResponseAPI()
        {
            RealAPI = new RaindropAPI(TestHttpClientFactory.Malformed, RealLogger);
            return this;
        }

        // Modern C# 12 HttpClient factory using primary constructor and static properties
        public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
        {
            public static TestHttpClientFactory Mock { get; } = new(() => new TestHttpMessageHandler());
            public static TestHttpClientFactory MissingFiles { get; } = new(() => new TestHttpMessageHandlerMissingFiles());
            public static TestHttpClientFactory RealAPI { get; } = new(() => new TestHttpMessageHandlerRealAPI());
            public static TestHttpClientFactory Failing { get; } = new(() => new TestHttpMessageHandlerFailing());
            public static TestHttpClientFactory Malformed { get; } = new(() => new TestHttpMessageHandlerMalformed());

            public HttpClient CreateClient(string name = "")
            {
                var client = new HttpClient(handlerFactory(), true);
                client.BaseAddress = new Uri("http://localhost/");
                return client;
            }
        }

        public void Dispose()
        {
            DummyAPI?.Dispose();
            RealAPI?.Dispose();
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
    }

    // No-op disposable for logger scopes
    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            // No operation
        }
    }

    // Test HTTP message handler for DummyRaindropAPI
    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Debug: Log the actual request URI
            Console.WriteLine($"TestHttpMessageHandler received request: {request.RequestUri}");
            Console.WriteLine($"  - AbsolutePath: {request.RequestUri?.AbsolutePath}");
            Console.WriteLine($"  - Query: {request.RequestUri?.Query}");
            Console.WriteLine($"  - Full URI: {request.RequestUri}");
            
            if (request.RequestUri?.AbsolutePath.Contains("/mockdata/videos.json") == true)
            {
                Console.WriteLine("  -> Returning videos JSON");
                var videosJson = CreateTestVideosJson();
                response.Content = new StringContent(videosJson, Encoding.UTF8, "application/json");
            }
            else if (request.RequestUri?.AbsolutePath.Contains("/mockdata/articles.json") == true)
            {
                Console.WriteLine("  -> Returning articles JSON");
                var articlesJson = CreateTestArticlesJson();
                response.Content = new StringContent(articlesJson, Encoding.UTF8, "application/json");
            }
            else
            {
                Console.WriteLine("  -> Returning 404 NotFound");
                response.StatusCode = HttpStatusCode.NotFound;
            }

            return Task.FromResult(response);
        }
    }

    // Test HTTP message handler that simulates missing files
    private sealed class TestHttpMessageHandlerMissingFiles : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"TestHttpMessageHandlerMissingFiles received request: {request.RequestUri}");
            Console.WriteLine("  -> Returning 404 NotFound (simulating missing files)");
            
            // Always return 404 to simulate missing files
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }

    // Test HTTP message handler for RaindropAPI
    private sealed class TestHttpMessageHandlerRealAPI : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            
            // Debug: Log the actual request URI
            Console.WriteLine($"TestHttpMessageHandlerRealAPI received request: {request.RequestUri}");
            Console.WriteLine($"  - AbsolutePath: {request.RequestUri?.AbsolutePath}");
            
            // Fix: Check for correct API endpoints
            if (request.RequestUri?.AbsolutePath.Contains("/api/RaindropListVideos") == true)
            {
                Console.WriteLine("  -> Returning videos JSON for RaindropAPI");
                var videosJson = CreateTestVideosJson();
                response.Content = new StringContent(videosJson, Encoding.UTF8, "application/json");
            }
            else if (request.RequestUri?.AbsolutePath.Contains("/api/RaindropListArticles") == true)
            {
                Console.WriteLine("  -> Returning articles JSON for RaindropAPI");
                var articlesJson = CreateTestArticlesJson();
                response.Content = new StringContent(articlesJson, Encoding.UTF8, "application/json");
            }
            else
            {
                Console.WriteLine($"  -> Returning 404 NotFound for unmatched path: {request.RequestUri?.AbsolutePath}");
                response.StatusCode = HttpStatusCode.NotFound;
            }

            return Task.FromResult(response);
        }
    }

    // Test HTTP message handler that simulates API failures
    private sealed class TestHttpMessageHandlerFailing : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException("Simulated API failure");
        }
    }

    // Test HTTP message handler that returns malformed responses
    private sealed class TestHttpMessageHandlerMalformed : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    // Helper methods to create test data
    private static string CreateTestVideosJson()
    {
        var videos = new[]
        {
            new RaindropItem
            {
                Id = 1,
                Title = "Test Video 1",
                Link = "https://example.com/video1",
                Type = "video",
                Excerpt = "Test video excerpt",
                Cover = "https://example.com/cover1.jpg",
                Created = DateTime.UtcNow,
                CollectionId = 1
            },
            new RaindropItem
            {
                Id = 2,
                Title = "Test Video 2",
                Link = "https://example.com/video2",
                Type = "video",
                Excerpt = "Another test video",
                Cover = "https://example.com/cover2.jpg",
                Created = DateTime.UtcNow,
                CollectionId = 1
            }
        };

        return JsonSerializer.Serialize(videos, RaindropJsonSerializerContext.DefaultOptions);
    }

    private static string CreateTestArticlesJson()
    {
        var articles = new[]
        {
            new RaindropItem
            {
                Id = 3,
                Title = "Test Article 1",
                Link = "https://example.com/article1",
                Type = "article",
                Excerpt = "Test article excerpt",
                Cover = "https://example.com/article-cover1.jpg",
                Created = DateTime.UtcNow,
                CollectionId = 2
            },
            new RaindropItem
            {
                Id = 4,
                Title = "Test Article 2",
                Link = "https://example.com/article2",
                Type = "article",
                Excerpt = "Another test article",
                Cover = "https://example.com/article-cover2.jpg",
                Created = DateTime.UtcNow,
                CollectionId = 2
            }
        };

        return JsonSerializer.Serialize(articles, RaindropJsonSerializerContext.DefaultOptions);
    }
}