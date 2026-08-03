using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Cache-first load of Raindrop articles.
/// </summary>
public sealed class LoadArticlesHandler(IRaindropAPI raindropApi, IRaindropItemsStorage storage)
    : IRequestHandler<LoadArticlesQuery, Result<RaindropItemsResponse>>
{
    private readonly IRaindropAPI _raindropApi = raindropApi ?? throw new ArgumentNullException(nameof(raindropApi));
    private readonly IRaindropItemsStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public ValueTask<Result<RaindropItemsResponse>> Handle(
        LoadArticlesQuery request,
        CancellationToken cancellationToken)
    {
        return new ValueTask<Result<RaindropItemsResponse>>(
            RaindropItemsUseCases.LoadAsync(
                _storage,
                _raindropApi.GetArticlesAsync,
                RaindropItemsUseCases.ArticlesCacheKey,
                cancellationToken));
    }
}
