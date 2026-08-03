using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Cache-first load of Raindrop videos.
/// </summary>
public sealed class LoadVideosHandler(IRaindropAPI raindropApi, IRaindropItemsStorage storage)
    : IRequestHandler<LoadVideosQuery, Result<RaindropItemsResponse>>
{
    private readonly IRaindropAPI _raindropApi = raindropApi ?? throw new ArgumentNullException(nameof(raindropApi));
    private readonly IRaindropItemsStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public ValueTask<Result<RaindropItemsResponse>> Handle(
        LoadVideosQuery request,
        CancellationToken cancellationToken)
    {
        return new ValueTask<Result<RaindropItemsResponse>>(
            RaindropItemsUseCases.LoadAsync(
                _storage,
                _raindropApi.GetVideosAsync,
                RaindropItemsUseCases.VideosCacheKey,
                cancellationToken));
    }
}
