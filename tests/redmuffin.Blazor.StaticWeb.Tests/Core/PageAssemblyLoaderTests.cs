using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed class PageAssemblyLoaderTests
{
    [Test]
    public async Task EnsureLoadedAsync_For_Unknown_PageKey_Completes_Without_Throwing()
    {
        var loader = CreateLoader();

        await loader.EnsureLoadedAsync("counter").ConfigureAwait(false);

        await Assert.That(loader.LoadedAssemblies.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchHomePrimaryJourneysAsync_Completes_Without_Throwing()
    {
        var loader = CreateLoader();

        // Catalog lists Components.dll + Raindrop.dll for Articles/Videos. Prefetch is speculative:
        // failures are swallowed; success path must also be safe when re-entered.
        await loader.PrefetchHomePrimaryJourneysAsync().ConfigureAwait(false);
        await loader.PrefetchHomePrimaryJourneysAsync().ConfigureAwait(false);
    }

    private static PageAssemblyLoader CreateLoader()
    {
        var jsRuntime = new JSRuntime_Stub();
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
