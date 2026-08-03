using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Tests;

[Category("Feature:Raindrop")]
public partial class IRaindropAPITests
{
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

    private sealed class TestScope : IDisposable
    {
        public Logger_Spy<DummyRaindropAPI> DummyLogger { get; } = new();
        public Logger_Spy<RaindropAPI> RealLogger { get; } = new();
        public DummyRaindropAPI? DummyAPI { get; private set; }
        public RaindropAPI? RealAPI { get; private set; }

        public Logger_Spy<DummyRaindropAPI> GetDummyLogger()
        {
            return DummyLogger;
        }

        public Logger_Spy<RaindropAPI> GetRealLogger()
        {
            return RealLogger;
        }

        public TestScope WithDummyAPI()
        {
            DummyAPI = new DummyRaindropAPI(HttpClientFactory_Stub.Mock, DummyLogger);
            ArgumentNullException.ThrowIfNull(DummyAPI);
            return this;
        }

        public TestScope WithDummyAPIMissingFiles()
        {
            DummyAPI = new DummyRaindropAPI(HttpClientFactory_Stub.MissingFiles, DummyLogger);
            ArgumentNullException.ThrowIfNull(DummyAPI);
            return this;
        }

        public TestScope WithRealAPI()
        {
            RealAPI = new RaindropAPI(HttpClientFactory_Stub.RealAPI, RealLogger);
            return this;
        }

        public TestScope WithFailingRealAPI()
        {
            RealAPI = new RaindropAPI(HttpClientFactory_Stub.Failing, RealLogger);
            return this;
        }

        public TestScope WithMalformedResponseAPI()
        {
            RealAPI = new RaindropAPI(HttpClientFactory_Stub.Malformed, RealLogger);
            return this;
        }

        public void Dispose()
        {
            DummyAPI?.Dispose();
            RealAPI?.Dispose();
        }

        private sealed class HttpClientFactory_Stub(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
        {
            public static HttpClientFactory_Stub Mock { get; } = new(() => new HttpMessageHandler_Stub());
            public static HttpClientFactory_Stub MissingFiles { get; } = new(() => new HttpMessageHandler_MissingFilesStub());
            public static HttpClientFactory_Stub RealAPI { get; } = new(() => new HttpMessageHandler_RealAPIStub());
            public static HttpClientFactory_Stub Failing { get; } = new(() => new HttpMessageHandler_FailingStub());
            public static HttpClientFactory_Stub Malformed { get; } = new(() => new HttpMessageHandler_MalformedStub());

            public HttpClient CreateClient(string name = "")
            {
                var client = new HttpClient(handlerFactory(), true);
                client.BaseAddress = new Uri("http://localhost/");
                return client;
            }
        }
    }

    private sealed class Logger_Spy<T> : ILogger<T>
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
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class HttpMessageHandler_Stub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(HttpStatusCode.OK);

            if (request.RequestUri?.AbsolutePath.Contains("/mockdata/videos.json") == true)
            {
                response.Content = new StringContent(CreateTestVideosJson(), Encoding.UTF8, "application/json");
            }
            else if (request.RequestUri?.AbsolutePath.Contains("/mockdata/articles.json") == true)
            {
                response.Content = new StringContent(CreateTestArticlesJson(), Encoding.UTF8, "application/json");
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
            }

            return Task.FromResult(response);
        }
    }

    private sealed class HttpMessageHandler_MissingFilesStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class HttpMessageHandler_RealAPIStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(HttpStatusCode.OK);

            if (request.RequestUri?.AbsolutePath.Contains("/api/RaindropListVideos") == true)
            {
                response.Content = new StringContent(CreateTestVideosJson(), Encoding.UTF8, "application/json");
            }
            else if (request.RequestUri?.AbsolutePath.Contains("/api/RaindropListArticles") == true)
            {
                response.Content = new StringContent(CreateTestArticlesJson(), Encoding.UTF8, "application/json");
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
            }

            return Task.FromResult(response);
        }
    }

    private sealed class HttpMessageHandler_FailingStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException("Simulated API failure");
        }
    }

    private sealed class HttpMessageHandler_MalformedStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
            });
        }
    }
}
