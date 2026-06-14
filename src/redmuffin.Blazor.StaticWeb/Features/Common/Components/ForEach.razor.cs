using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class ForEach<TItem>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    [Parameter, EditorRequired]
    public RenderFragment<TItem> ChildContent { get; set; } = default!;
}
