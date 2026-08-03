using Mediator;
using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Cache-first load of Raindrop articles (progressive paint).
/// </summary>
public sealed record LoadArticlesQuery : IRequest<Result<RaindropItemsResponse>>;
