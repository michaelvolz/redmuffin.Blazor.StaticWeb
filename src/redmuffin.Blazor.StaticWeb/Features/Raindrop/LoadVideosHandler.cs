using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop;

/// <summary>
///     Eager Mediator handler so SourceGen does not root the Raindrop implementation assembly at boot.
/// </summary>
public sealed class LoadVideosHandler(IRaindropItemsFacade facade)
    : IRequestHandler<LoadVideosQuery, Result<RaindropItemsResponse>>
{
    private readonly IRaindropItemsFacade _facade = facade ?? throw new ArgumentNullException(nameof(facade));

    public async ValueTask<Result<RaindropItemsResponse>> Handle(
        LoadVideosQuery request,
        CancellationToken cancellationToken)
    {
        return await _facade.LoadVideosAsync(cancellationToken).ConfigureAwait(false);
    }
}