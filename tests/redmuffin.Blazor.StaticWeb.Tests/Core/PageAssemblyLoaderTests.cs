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

        // Key must stay outside PageAssemblyCatalog (sample/demo keys are now wired).
        await loader.EnsureLoadedAsync("unknown-route").ConfigureAwait(false);

        await Assert.That(loader.LoadedAssemblies.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefetchHomePrimaryJourneysAsync_Loads_Home_Assemblies_And_Is_Idempotent()
    {
        var loader = CreateLoader();

        await loader.PrefetchHomePrimaryJourneysAsync().ConfigureAwait(false);

        // Non-browser hosts resolve catalog DLL names through Assembly.Load, so the loaded set is observable here.
        var loadedNames = loader
            .LoadedAssemblies.Select(assembly => assembly.GetName().Name)
            .ToList();
        using (Assert.Multiple())
        {
            await Assert.That(loadedNames).Contains("Articles");
            await Assert.That(loadedNames).Contains("Videos");
            await Assert.That(loadedNames).Contains("Components");
            await Assert.That(loadedNames).Contains("Raindrop");
            await Assert.That(loadedNames.Distinct().Count()).IsEqualTo(loadedNames.Count);
        }

        var countAfterFirstPrefetch = loader.LoadedAssemblies.Count;

        await loader.PrefetchHomePrimaryJourneysAsync().ConfigureAwait(false);

        await Assert.That(loader.LoadedAssemblies.Count).IsEqualTo(countAfterFirstPrefetch);
    }

    private static PageAssemblyLoader CreateLoader()
    {
        var jsRuntime = new JSRuntime_Stub();
        return new PageAssemblyLoader(
            new LazyAssemblyLoader(jsRuntime),
            jsRuntime,
            NullLogger<PageAssemblyLoader>.Instance
        );
    }

    private sealed class JSRuntime_Stub : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        )
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
