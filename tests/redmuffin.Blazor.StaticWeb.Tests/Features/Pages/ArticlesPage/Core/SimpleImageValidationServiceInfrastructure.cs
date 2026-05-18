using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Models;
using redmuffin.Blazor.StaticWeb.Services;
using SimpleImageValidationService = redmuffin.Blazor.StaticWeb.Features.Pages.ArticlesPage.Core.Services.SimpleImageValidationService;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage.Core;

public sealed class SimpleImageValidationServiceInfrastructure : IDisposable
{
    private readonly ControlledHttpHandler_Fake _handler;
    private readonly BrowserStorage_Stub _browserStorage;
    private readonly SimpleImageValidationService _service;

    public SimpleImageValidationServiceInfrastructure()
    {
        _handler = new ControlledHttpHandler_Fake();
        _browserStorage = new BrowserStorage_Stub();
        var factory = new TestHttpClientFactory(_handler);
        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.None));
        _service = new SimpleImageValidationService(
            factory,
            _browserStorage,
            loggerFactory.CreateLogger<SimpleImageValidationService>());
    }

    public SimpleImageValidationService Service => _service;

    public void SetupResponse(string url, HttpStatusCode status, string content, string contentType = "image/jpeg")
    {
        _handler.Responses[url] = new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        };
    }

    public void SetupNetworkError(string url, Exception exception)
    {
        _handler.Errors[url] = exception;
    }

    public BrowserStorage_Stub BrowserStorage => _browserStorage;

    public void Dispose() => _handler.Dispose();

    /// <summary>Pre-populates the cache for a given image URL.</summary>
    public void SetupCachedResult(string imageUrl, ImageValidationResult? result)
    {
        var cacheKey = SimpleImageValidationService.GetCacheKey(imageUrl);
        _browserStorage.CachedResults[cacheKey] = result;
    }

    private sealed class ControlledHttpHandler_Fake : HttpMessageHandler
    {
        public Dictionary<string, HttpResponseMessage> Responses { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Exception> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri?.ToString() ?? string.Empty;

            if (Errors.TryGetValue(key, out var error))
                return Task.FromException<HttpResponseMessage>(error);

            if (Responses.TryGetValue(key, out var response))
                return Task.FromResult(response);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found", Encoding.UTF8, "text/plain")
            });
        }
    }

    public sealed class BrowserStorage_Stub : IBrowserStorageService
    {
        public Dictionary<string, object?> CachedResults { get; } = [];

        public Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (CachedResults.TryGetValue(key, out var result) && result is T typed)
                return Task.FromResult<T?>(typed);
            return Task.FromResult<T?>(default);
        }

        public Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            CachedResults[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            CachedResults.Remove(key);
            return Task.CompletedTask;
        }

        public Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StorageStats
            {
                TotalItems = 100,
                TotalSizeBytes = 10_000_000,
                QuotaLimitBytes = 100_000_000,
                QuotaUsagePercent = 10,
            });

        public Task<int> EvictLeastRecentlyUsedAsync(long targetSizeBytes, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<string>> GetKeysAsync(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<string>());
        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<long> GetItemSizeAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(0L);
        public void SetQuotaLimit(long quotaBytes) { }
        public long GetQuotaLimit() => 100_000_000;
        public Task<int> ClearAllStorageAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
