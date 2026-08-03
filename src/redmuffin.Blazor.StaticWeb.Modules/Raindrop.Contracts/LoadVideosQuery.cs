using Mediator;
using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Cache-first load of Raindrop videos (progressive paint).
/// </summary>
public sealed record LoadVideosQuery : IRequest<Result<RaindropItemsResponse>>;
