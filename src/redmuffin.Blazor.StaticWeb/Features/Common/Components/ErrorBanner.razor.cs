using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

public partial class ErrorBanner
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;
}
