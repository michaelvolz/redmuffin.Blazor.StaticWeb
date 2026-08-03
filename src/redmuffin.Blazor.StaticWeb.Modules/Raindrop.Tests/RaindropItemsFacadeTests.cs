using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Tests;

[Category("Feature:Raindrop")]
[Category("Unit")]
public sealed class RaindropItemsFacadeTests
{
    [Test]
    public async Task Load_returns_cached_items_without_calling_api_when_cache_hit()
    {
        var cached = CreateItems("Cached");
        var api = new RaindropAPI_Fake();
        var storage = new RaindropItemsStorage_Fake { GetResult = cached };
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.LoadArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.IsFromCache).IsTrue();
            await Assert.That(result.Value.Items[0].Title).IsEqualTo("Cached");
            await Assert.That(api.ArticlesCallCount).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Load_fetches_and_caches_when_cache_miss()
    {
        var fresh = CreateItems("Fresh");
        var api = new RaindropAPI_Fake { ArticlesResult = Result.Success(fresh) };
        var storage = new RaindropItemsStorage_Fake();
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.LoadArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.IsFromCache).IsFalse();
            await Assert.That(result.Value.Items[0].Title).IsEqualTo("Fresh");
            await Assert.That(api.ArticlesCallCount).IsEqualTo(1);
            await Assert.That(storage.LastSetKey).IsEqualTo(RaindropItemsUseCases.ArticlesCacheKey);
            await Assert.That(storage.LastSetItems![0].Title).IsEqualTo("Fresh");
        }
    }

    [Test]
    public async Task Load_returns_failure_when_cache_miss_and_api_fails()
    {
        var api = new RaindropAPI_Fake
        {
            ArticlesResult = Result.Failure<IReadOnlyList<RaindropItem>>("Network down")
        };
        var storage = new RaindropItemsStorage_Fake();
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.LoadArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(result.Error).IsEqualTo("Network down");
            await Assert.That(storage.LastSetItems).IsNull();
        }
    }

    [Test]
    public async Task Refresh_fetches_even_when_cache_has_items()
    {
        var cached = CreateItems("Cached");
        var fresh = CreateItems("Refreshed");
        var api = new RaindropAPI_Fake { ArticlesResult = Result.Success(fresh) };
        var storage = new RaindropItemsStorage_Fake { GetResult = cached };
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.RefreshArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.IsFromCache).IsFalse();
            await Assert.That(result.Value.Items[0].Title).IsEqualTo("Refreshed");
            await Assert.That(api.ArticlesCallCount).IsEqualTo(1);
            await Assert.That(storage.LastSetItems![0].Title).IsEqualTo("Refreshed");
        }
    }

    [Test]
    public async Task Refresh_returns_success_when_cache_write_throws()
    {
        var fresh = CreateItems("Fresh");
        var api = new RaindropAPI_Fake { ArticlesResult = Result.Success(fresh) };
        var storage = new RaindropItemsStorage_Fake { ThrowOnSet = true };
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.RefreshArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Items[0].Title).IsEqualTo("Fresh");
        }
    }

    [Test]
    public async Task Load_videos_uses_videos_cache_key()
    {
        var fresh = CreateItems("Video");
        var api = new RaindropAPI_Fake { VideosResult = Result.Success(fresh) };
        var storage = new RaindropItemsStorage_Fake();
        var facade = new RaindropItemsFacade(api, storage);

        var result = await facade.LoadVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(api.VideosCallCount).IsEqualTo(1);
            await Assert.That(storage.LastSetKey).IsEqualTo(RaindropItemsUseCases.VideosCacheKey);
        }
    }

    private static IReadOnlyList<RaindropItem> CreateItems(string title) =>
    [
        new()
        {
            Id = 1,
            Title = title,
            Link = "https://example.com",
            Type = "article"
        }
    ];

    private sealed class RaindropAPI_Fake : IRaindropAPI
    {
        public Result<IReadOnlyList<RaindropItem>> ArticlesResult { get; set; } =
            Result.Success<IReadOnlyList<RaindropItem>>([]);

        public Result<IReadOnlyList<RaindropItem>> VideosResult { get; set; } =
            Result.Success<IReadOnlyList<RaindropItem>>([]);

        public int ArticlesCallCount { get; private set; }
        public int VideosCallCount { get; private set; }

        public Task<Result<IReadOnlyList<RaindropItem>>> GetArticlesAsync(CancellationToken cancellationToken = default)
        {
            ArticlesCallCount++;
            return Task.FromResult(ArticlesResult);
        }

        public Task<Result<IReadOnlyList<RaindropItem>>> GetVideosAsync(CancellationToken cancellationToken = default)
        {
            VideosCallCount++;
            return Task.FromResult(VideosResult);
        }
    }

    private sealed class RaindropItemsStorage_Fake : IRaindropItemsStorage
    {
        public IReadOnlyList<RaindropItem>? GetResult { get; set; }
        public string? LastSetKey { get; private set; }
        public IReadOnlyList<RaindropItem>? LastSetItems { get; private set; }
        public bool ThrowOnSet { get; set; }

        public Task<IReadOnlyList<RaindropItem>?> TryGetAsync(string cacheKey, CancellationToken cancellationToken = default)
            => Task.FromResult(GetResult);

        public Task SetAsync(string cacheKey, IReadOnlyList<RaindropItem> items, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSet)
                throw new InvalidOperationException("storage unavailable");

            LastSetKey = cacheKey;
            LastSetItems = items;
            return Task.CompletedTask;
        }
    }
}