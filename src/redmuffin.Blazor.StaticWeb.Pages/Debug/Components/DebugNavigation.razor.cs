using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug.Components;

public partial class DebugNavigation : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string GetActiveClass(string path)
    {
        var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
        return currentPath.Equals(path.TrimStart('/'), StringComparison.OrdinalIgnoreCase) ? "active" : string.Empty;
    }
}