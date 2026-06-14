using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class Conditional
{
    [Parameter, EditorRequired]
    public bool Condition { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
