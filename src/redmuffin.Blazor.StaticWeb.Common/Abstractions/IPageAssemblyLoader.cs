using System.Reflection;

namespace redmuffin.Blazor.StaticWeb.Common.Abstractions;

/// <summary>
///     Loads page-lazy assemblies (navigate or prefetch) with per-page memoization.
///     Empty catalog entries no-op until conversion fills them.
/// </summary>
public interface IPageAssemblyLoader
{
    /// <summary>
    ///     Assemblies loaded so far for <c>Router.AdditionalAssemblies</c>.
    /// </summary>
    IReadOnlyList<Assembly> LoadedAssemblies { get; }

    /// <summary>
    ///     Ensures assemblies for <paramref name="pageKey"/> are loaded (no-op when catalog empty).
    /// </summary>
    Task EnsureLoadedAsync(string pageKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     After Home is interactive: prefetch Articles + Videos batches only.
    ///     Silent on failure; respects Save-Data when available.
    /// </summary>
    Task PrefetchHomePrimaryJourneysAsync(CancellationToken cancellationToken = default);
}
