using Mediator;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

public sealed record GetHelloQuery : IRequest<HelloResponse>;
