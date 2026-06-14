using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class EmptyState
{
    public const string EmptyStateElementId = "empty-state";

    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;
}
