using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Common.Components;

/// <summary>
///     Code-behind for RefreshBadge component.
/// </summary>
public partial class RefreshBadge : ComponentBase
{
    [Parameter] public RefreshBadgeState State { get; set; } = RefreshBadgeState.Hidden;
    [Parameter] public string Text { get; set; } = string.Empty;
    [Parameter] public string CssClass { get; set; } = string.Empty;
    [Parameter] public EventCallback OnClick { get; set; }

    private bool IsDisabled => State == RefreshBadgeState.Loading;

    private async Task HandleClickAsync()
    {
        if (!IsDisabled) await OnClick.InvokeAsync().ConfigureAwait(false);
    }

    private string GetCssClass()
    {
        var classes = new List<string>();

        classes.Add(State switch
        {
            RefreshBadgeState.Hidden => "refresh-badge--hidden",
            RefreshBadgeState.Visible => "refresh-badge--visible",
            RefreshBadgeState.Loading => "refresh-badge--loading",
            RefreshBadgeState.Error => "refresh-badge--error",
            _ => string.Empty
        });

        if (!string.IsNullOrEmpty(CssClass))
            classes.Add(CssClass);

        return string.Join(" ", classes.Where(c => !string.IsNullOrEmpty(c)));
    }

    private string GetIconClass()
    {
        return State switch
        {
            RefreshBadgeState.Loading => "fas fa-spinner fa-spin",
            RefreshBadgeState.Error => "fas fa-exclamation-triangle",
            _ => "fas fa-sync-alt"
        };
    }

    private string GetTooltip()
    {
        return State switch
        {
            RefreshBadgeState.Visible => "Click to refresh content",
            RefreshBadgeState.Loading => "Refreshing...",
            RefreshBadgeState.Error => "Refresh failed - click to retry",
            _ => "Refresh"
        };
    }
}