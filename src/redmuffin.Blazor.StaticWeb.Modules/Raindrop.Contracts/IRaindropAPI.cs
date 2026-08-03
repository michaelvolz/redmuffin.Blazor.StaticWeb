using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Abstraction for RaindropIO API operations — real HTTP or dummy JSON for local development.
///     Module-internal port: pages must not inject this once Mediator load/refresh handlers exist.
/// </summary>
public interface IRaindropAPI
{
    /// <summary>
    ///     Retrieves video items from the RaindropIO API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Success with the video items, or failure for expected network/response problems.
    ///     Cancellation remains exceptional.
    /// </returns>
    Task<Result<IReadOnlyList<RaindropItem>>> GetVideosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves article items from the RaindropIO API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Success with the article items, or failure for expected network/response problems.
    ///     Cancellation remains exceptional.
    /// </returns>
    Task<Result<IReadOnlyList<RaindropItem>>> GetArticlesAsync(CancellationToken cancellationToken = default);
}
