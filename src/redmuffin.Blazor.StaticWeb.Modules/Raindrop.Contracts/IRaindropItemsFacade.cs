using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Module application surface for Raindrop list load/refresh.
///     Host-eager Mediator handlers call this; implementations live in the (optionally lazy) module assembly.
/// </summary>
public interface IRaindropItemsFacade
{
    Task<Result<RaindropItemsResponse>> LoadArticlesAsync(CancellationToken cancellationToken = default);

    Task<Result<RaindropItemsResponse>> RefreshArticlesAsync(CancellationToken cancellationToken = default);

    Task<Result<RaindropItemsResponse>> LoadVideosAsync(CancellationToken cancellationToken = default);

    Task<Result<RaindropItemsResponse>> RefreshVideosAsync(CancellationToken cancellationToken = default);
}