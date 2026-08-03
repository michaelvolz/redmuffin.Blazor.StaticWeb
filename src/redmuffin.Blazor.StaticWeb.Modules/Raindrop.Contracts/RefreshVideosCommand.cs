using Mediator;
using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Force network refresh of Raindrop videos and update cache.
/// </summary>
public sealed record RefreshVideosCommand : IRequest<Result<RaindropItemsResponse>>;
