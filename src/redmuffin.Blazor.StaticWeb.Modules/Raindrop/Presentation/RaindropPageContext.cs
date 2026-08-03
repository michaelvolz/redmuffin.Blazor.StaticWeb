using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Presentation;

/// <summary>
///     Mutable UI state for a Raindrop list page (Articles or Videos).
///     The page owns one instance and maps Mediator results into it.
/// </summary>
public sealed class RaindropPageContext
{
    public IReadOnlyList<RaindropItem>? Items { get; set; }

    public IDictionary<string, string> ImageUrlCache { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string? ErrorMessage { get; set; }

    public RefreshBadgeState BadgeState { get; set; } = RefreshBadgeState.Hidden;

    public bool IsRefreshing { get; set; }
}