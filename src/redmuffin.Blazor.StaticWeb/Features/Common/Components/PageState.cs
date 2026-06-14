using System.Diagnostics;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public abstract record PageState<T>
{
    public TResult Match<TResult>(
        Func<TResult> onLoading,
        Func<string, TResult> onErrored,
        Func<T, TResult> onData) => this switch
        {
            Loading => onLoading(),
            Errored(var message) => onErrored(message),
            Data(var value) => onData(value),
            _ => throw new UnreachableException($"Unknown PageState subtype: {GetType().Name}")
        };

    public sealed record Loading : PageState<T>;

    public sealed record Errored(string Message) : PageState<T>;

    public sealed record Data(T Value) : PageState<T>;
}
