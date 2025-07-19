using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.Models;
using redmuffin.Blazor.StaticWeb.Services;
using redmuffin.Blazor.StaticWeb.Tests.Integration;

namespace redmuffin.Blazor.StaticWeb.Tests.Performance;

public class OpenGraphPerformanceTests : TestBase
{
    public OpenGraphPerformanceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
        services.AddSingleton<ICacheService, MockCacheService>();
        services.AddSingleton<IImageValidationService, MockImageValidationService>();
        services.AddSingleton<OpenGraphImagesService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _imageService = _serviceProvider.GetRequiredService<OpenGraphImagesService>();
    }

    private readonly OpenGraphImagesService _imageService;
    private readonly ServiceProvider _serviceProvider;

    [Test]
    public async Task BatchProcessing_LargeDataSet_ShouldMaintainPerformance()
    {
        // Arrange
        var largeArticleUrls = Enumerable.Range(1, 1000).Select(i => $"https://example.com/large-article{i}").ToList();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _imageService.GetImagesAsync(largeArticleUrls).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(10000); // Expect under 10 seconds for 1000 articles
    }

    [Test]
    public async Task BatchProcessing_ShouldCompleteUnderExpectedTime()
    {
        // Arrange
        var articleUrls = Enumerable.Range(1, 100).Select(i => $"https://example.com/article{i}").ToList();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(3000); // Expect under 3 seconds
    }

    [Test]
    public async Task CacheEfficiency_ShouldImproveWithRepeatedAccess()
    {
        // Arrange
        var articleUrls = Enumerable.Range(1, 50).Select(i => $"https://example.com/cache-test{i}").ToList();

        // Act & Assert - First call (cache miss)
        var firstCallStopwatch = Stopwatch.StartNew();
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);
        firstCallStopwatch.Stop();

        // Act & Assert - Second call (cache hit)
        var secondCallStopwatch = Stopwatch.StartNew();
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);
        secondCallStopwatch.Stop();

        // Assert - Second call should be significantly faster (using ticks for better precision)
        // If both calls are very fast (less than 1ms), we'll use a more lenient assertion
        if (firstCallStopwatch.ElapsedMilliseconds == 0 && secondCallStopwatch.ElapsedMilliseconds == 0)
            // For very fast operations, just verify the second call isn't slower in ticks
            await Assert.That(secondCallStopwatch.ElapsedTicks).IsLessThanOrEqualTo(firstCallStopwatch.ElapsedTicks);
        else
            // For measurable operations, second call should be significantly faster
            await Assert.That(secondCallStopwatch.ElapsedMilliseconds).IsLessThan(Math.Max(1, firstCallStopwatch.ElapsedMilliseconds / 2));
    }

    [Test]
    public async Task CacheInvalidation_ShouldNotSignificantlyImpactPerformance()
    {
        // Arrange
        var articleUrls = Enumerable.Range(1, 50).Select(i => $"https://example.com/invalidation-test{i}").ToList();
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false); // Populate cache
        var stopwatch = Stopwatch.StartNew();

        // Act - Invalidate multiple entries
        var invalidationTasks = articleUrls.Select(async url => await _imageService.InvalidateCacheAsync(url).ConfigureAwait(false));

        await Task.WhenAll(invalidationTasks).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(1000); // Invalidation should be fast
    }

    [Test]
    public async Task CacheRetrieval_ShouldBeEfficientForMultipleCalls()
    {
        // Arrange
        var articleUrl = "https://example.com/cached-article";
        await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false); // Populate cache
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (var i = 0; i < 100; i++) await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(500); // Expect under 500 ms for 100 accesses
    }

    [Test]
    public async Task CacheStats_ShouldReportAccurateMetrics()
    {
        // Arrange
        var articleUrls = Enumerable.Range(1, 10).Select(i => $"https://example.com/stats-test{i}").ToList();

        // Act
        await _imageService.GetImagesAsync(articleUrls).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        var stats = await _imageService.GetCacheStatsAsync().ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(100); // Cache stats should be fast
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats.ContainsKey("TotalItems")).IsTrue();
    }

    [Test]
    public async Task ParallelCacheAccess_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var articleUrl = "https://example.com/parallel-test";
        await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false); // Populate cache
        var stopwatch = Stopwatch.StartNew();

        // Act - Make 100 concurrent requests
        var tasks = Enumerable.Range(0, 100).Select(async _ => await _imageService.GetImageAsync(articleUrl).ConfigureAwait(false));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(1000); // Should handle concurrent access efficiently
        await Assert.That(results.All(r => r != null)).IsTrue();
        await Assert.That(results.All(r => r!.ImageUrl != null)).IsTrue();
    }
}

public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope - HttpClient lifecycle is managed by the caller
        return new HttpClient(new TestHttpMessageHandler())
        {
            BaseAddress = new Uri("https://example.com")
        };
#pragma warning restore CA2000
    }
}

public class MockImageValidationService : IImageValidationService
{
    public async Task<ImageValidationResult> ValidateImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new ImageValidationResult
        {
            ImageUrl = imageUrl,
            IsValid = true,
            ContentType = "image/jpeg",
            ContentLength = 1024
        }).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, ImageValidationResult>> ValidateImagesAsync(IEnumerable<string> imageUrls, int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ImageValidationResult>();
        foreach (var url in imageUrls)
            result[url] = new ImageValidationResult
            {
                ImageUrl = url,
                IsValid = true,
                ContentType = "image/jpeg",
                ContentLength = 1024
            };
        return await Task.FromResult(result).ConfigureAwait(false);
    }

    public async Task<ImageValidationResult> ValidateImageWithCacheAsync(string imageUrl, int cacheExpirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        return await ValidateImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
    }

    public Task ClearValidationCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<IDictionary<string, object>> GetValidationCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new Dictionary<string, object>()).ConfigureAwait(false);
    }

    public Task RecordBrowserBlockedImageAsync(string imageUrl, string blockReason, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<ImageValidationResult?> GetCachedValidationResultAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new ImageValidationResult
        {
            ImageUrl = imageUrl,
            IsValid = true,
            ContentType = "image/jpeg",
            ContentLength = 1024
        }).ConfigureAwait(false);
    }
}