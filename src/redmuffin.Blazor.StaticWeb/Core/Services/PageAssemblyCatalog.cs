namespace redmuffin.Blazor.StaticWeb.Core.Services;

/// <summary>
///     Maps route page keys to the full lazy <b>need-set</b> (page + module +
///     component implementation DLLs) for navigate-time load and Home prefetch.
///     Product impl assemblies are lazy by default regardless of home; Contracts
///     stay eager. Empty lists are intentional no-ops until that route is wired.
/// </summary>
public static class PageAssemblyCatalog
{
    public const string ArticlesPageKey = "articles";
    public const string VideosPageKey = "videos";
    public const string ApiHealthPageKey = "api-health";

    /// <summary>
    ///     Page keys Home may prefetch after interactive — Articles and Videos only.
    /// </summary>
    public static readonly IReadOnlyList<string> HomePrefetchPageKeys =
    [
        ArticlesPageKey,
        VideosPageKey
    ];

    private static readonly Dictionary<string, string[]> AssembliesByPageKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ArticlesPageKey] = ["Articles.dll", "Components.dll", "Raindrop.dll"],
            [VideosPageKey] = ["Videos.dll", "Components.dll", "Raindrop.dll"],
            [ApiHealthPageKey] = ["ApiHealth.Page.dll", "AzureHealthCheck.dll"]
        };

    public static bool TryGetAssemblies(string pageKey, out IReadOnlyList<string> assemblyFileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        if (AssembliesByPageKey.TryGetValue(pageKey, out var names))
        {
            assemblyFileNames = names;
            return true;
        }

        assemblyFileNames = [];
        return false;
    }

    public static bool HasAssemblies(string pageKey)
    {
        return TryGetAssemblies(pageKey, out var names) && names.Count > 0;
    }

    /// <summary>
    ///     Maps a router path (no leading slash required) to a catalog page key.
    ///     Used by <c>OnNavigateAsync</c> so filling the catalog activates deep links
    ///     without a second App rewrite.
    /// </summary>
    /// <param name="path">Route path from the router (query string optional).</param>
    /// <param name="pageKey">Catalog page key when mapping succeeds; otherwise empty.</param>
    /// <returns><see langword="true" /> when the path maps to a known page key.</returns>
    public static bool TryGetPageKeyFromPath(string path, out string pageKey)
    {
        ArgumentNullException.ThrowIfNull(path);

        var trimmed = path.Trim('/');
        var queryIndex = trimmed.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
            trimmed = trimmed[..queryIndex];

        if (trimmed.Equals("articles", StringComparison.OrdinalIgnoreCase))
        {
            pageKey = ArticlesPageKey;
            return true;
        }

        if (trimmed.Equals("videos", StringComparison.OrdinalIgnoreCase))
        {
            pageKey = VideosPageKey;
            return true;
        }

        if (trimmed.Equals("api-health", StringComparison.OrdinalIgnoreCase))
        {
            pageKey = ApiHealthPageKey;
            return true;
        }

        pageKey = string.Empty;
        return false;
    }
}
