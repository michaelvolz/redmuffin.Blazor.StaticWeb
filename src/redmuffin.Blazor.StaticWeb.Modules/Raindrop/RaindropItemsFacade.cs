using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Module implementation of Raindrop load/refresh use cases.
/// </summary>
internal sealed class RaindropItemsFacade(IRaindropAPI raindropApi, IRaindropItemsStorage storage) : IRaindropItemsFacade
{
    private readonly IRaindropAPI _raindropApi = raindropApi ?? throw new ArgumentNullException(nameof(raindropApi));
    private readonly IRaindropItemsStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public Task<Result<RaindropItemsResponse>> LoadArticlesAsync(CancellationToken cancellationToken = default)
    {
        return RaindropItemsUseCases.LoadAsync(
            _storage,
            _raindropApi.GetArticlesAsync,
            RaindropItemsUseCases.ArticlesCacheKey,
            cancellationToken);
    }

    public Task<Result<RaindropItemsResponse>> RefreshArticlesAsync(CancellationToken cancellationToken = default)
    {
        return RaindropItemsUseCases.RefreshAsync(
            _storage,
            _raindropApi.GetArticlesAsync,
            RaindropItemsUseCases.ArticlesCacheKey,
            cancellationToken);
    }

    public Task<Result<RaindropItemsResponse>> LoadVideosAsync(CancellationToken cancellationToken = default)
    {
        return RaindropItemsUseCases.LoadAsync(
            _storage,
            _raindropApi.GetVideosAsync,
            RaindropItemsUseCases.VideosCacheKey,
            cancellationToken);
    }

    public Task<Result<RaindropItemsResponse>> RefreshVideosAsync(CancellationToken cancellationToken = default)
    {
        return RaindropItemsUseCases.RefreshAsync(
            _storage,
            _raindropApi.GetVideosAsync,
            RaindropItemsUseCases.VideosCacheKey,
            cancellationToken);
    }
}