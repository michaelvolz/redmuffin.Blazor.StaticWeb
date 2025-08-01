namespace redmuffin.Blazor.StaticWeb.Features.Cache.Enums;

/// <summary>
///     Represents the different states of the refresh badge component.
/// </summary>
public enum RefreshBadgeState
{
    /// <summary>
    ///     The badge is hidden from view.
    /// </summary>
    Hidden = 0,

    /// <summary>
    ///     The badge is visible and clickable.
    /// </summary>
    Visible = 1,

    /// <summary>
    ///     The badge is in a loading state during refresh operation.
    /// </summary>
    Loading = 2,

    /// <summary>
    ///     The badge is in an error state after a failed refresh operation.
    /// </summary>
    Error = 3
}