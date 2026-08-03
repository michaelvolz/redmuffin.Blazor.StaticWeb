using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed class PageAssemblyLoaderTests
{
    [Test]
    public async Task EnsureLoadedAsync_For_Empty_Catalog_Entries_Completes_Without_Throwing()
    {
        var loader = CreateLoader();

        await loader.EnsureLoadedAsync(PageAssemblyCatalog.ArticlesPageKey).ConfigureAwait(false);
        await loader.EnsureLoadedAsync(PageAssemblyCatalog.VideosPageKey).ConfigureAwait(false);

        await Assert.That(loader.LoadedAssemblies.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchHomePrimaryJourneysAsync_With_Empty_Catalog_Completes_Without_Throwing()
    {
        var loader = CreateLoader();

        await loader.PrefetchHomePrimaryJourneysAsync().ConfigureAwait(false);

        await Assert.That(loader.LoadedAssemblies.Count).IsEqualTo(0);
    }

    private static PageAssemblyLoader CreateLoader()
    {
        var jsRuntime = new JSRuntime_Stub();
        // LazyAssemblyLoader is never invoked for empty catalog keys.
        return new PageAssemblyLoader(
            new LazyAssemblyLoader(jsRuntime),
            jsRuntime,
            NullLogger<PageAssemblyLoader>.Instance);
    }

    private sealed class JSRuntime_Stub : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
