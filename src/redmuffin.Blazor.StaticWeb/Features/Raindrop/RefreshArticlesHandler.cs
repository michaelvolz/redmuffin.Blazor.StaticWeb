using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop;

/// <summary>
///     Eager Mediator handler so SourceGen does not root the Raindrop implementation assembly at boot.
/// </summary>
public sealed class RefreshArticlesHandler(IRaindropItemsFacade facade)
    : IRequestHandler<RefreshArticlesCommand, Result<RaindropItemsResponse>>
{
    private readonly IRaindropItemsFacade _facade = facade ?? throw new ArgumentNullException(nameof(facade));

    public async ValueTask<Result<RaindropItemsResponse>> Handle(
        RefreshArticlesCommand request,
        CancellationToken cancellationToken)
    {
        return await _facade.RefreshArticlesAsync(cancellationToken).ConfigureAwait(false);
    }
}