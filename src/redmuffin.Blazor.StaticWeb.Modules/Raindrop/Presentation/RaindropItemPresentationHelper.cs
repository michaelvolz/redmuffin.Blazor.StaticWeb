using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Presentation;

public static class RaindropItemPresentationHelper
{
    public static string DisplayTitle(RaindropItem item)
    {
        return string.IsNullOrEmpty(item.Title) ? "No Title Available" : item.Title;
    }

    public static string DisplayExcerpt(RaindropItem item)
    {
        if (string.IsNullOrEmpty(item.Excerpt))
            return "No Excerpt Available";

        return item.Excerpt.Length > 250
            ? string.Concat(item.Excerpt[..250], "...")
            : item.Excerpt;
    }
}
