using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Common.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Presentation;

/// <summary>
///     Mutable state shared between a Raindrop page component and the orchestrator.
///     The page component owns one instance. The orchestrator reads and writes it
///     through the orchestrator's static methods.
/// </summary>
public sealed class RaindropPageContext
{
    public IReadOnlyList<RaindropItem>? Items { get; set; }

    public IDictionary<string, string> ImageUrlCache { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string? ErrorMessage { get; set; }

    public RefreshBadgeState BadgeState { get; set; } = RefreshBadgeState.Hidden;

    public bool IsRefreshing { get; set; }
}
