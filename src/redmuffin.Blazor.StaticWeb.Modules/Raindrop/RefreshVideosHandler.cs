using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Force network refresh of Raindrop videos and update storage.
/// </summary>
public sealed class RefreshVideosHandler(IRaindropAPI raindropApi, IRaindropItemsStorage storage)
    : IRequestHandler<RefreshVideosCommand, Result<RaindropItemsResponse>>
{
    private readonly IRaindropAPI _raindropApi = raindropApi ?? throw new ArgumentNullException(nameof(raindropApi));
    private readonly IRaindropItemsStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public ValueTask<Result<RaindropItemsResponse>> Handle(
        RefreshVideosCommand request,
        CancellationToken cancellationToken)
    {
        return new ValueTask<Result<RaindropItemsResponse>>(
            RaindropItemsUseCases.RefreshAsync(
                _storage,
                _raindropApi.GetVideosAsync,
                RaindropItemsUseCases.VideosCacheKey,
                cancellationToken));
    }
}
