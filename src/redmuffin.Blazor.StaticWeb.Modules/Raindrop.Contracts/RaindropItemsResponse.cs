using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

/// <summary>
///     Progressive-capable payload for Raindrop load and refresh use cases.
/// </summary>
/// <param name="Items">Items to render (may be empty).</param>
/// <param name="IsFromCache">True when items were served from cache without a fresh network list.</param>
/// <param name="HasUpdateAvailable">True when newer data is ready (e.g. background refresh badge).</param>
public sealed record RaindropItemsResponse(
    IReadOnlyList<RaindropItem> Items,
    bool IsFromCache,
    bool HasUpdateAvailable);
