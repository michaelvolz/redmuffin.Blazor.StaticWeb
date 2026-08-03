using Mediator;
using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

public sealed record GetHelloQuery : IRequest<Result<HelloResponse>>;
