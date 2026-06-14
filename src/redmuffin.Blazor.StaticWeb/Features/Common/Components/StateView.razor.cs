using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class StateView<T>
{
    [Parameter, EditorRequired]
    public PageState<T> State { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment LoadingTemplate { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment<string> ErrorTemplate { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment<T> DataTemplate { get; set; } = default!;
}
