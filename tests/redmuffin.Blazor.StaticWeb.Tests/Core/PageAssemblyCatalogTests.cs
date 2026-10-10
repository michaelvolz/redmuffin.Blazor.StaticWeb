using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed class PageAssemblyCatalogTests
{
    [Test]
    [Arguments(PageAssemblyCatalog.ArticlesPageKey, null)]
    [Arguments(PageAssemblyCatalog.VideosPageKey, null)]
    [Arguments(PageAssemblyCatalog.ApiHealthPageKey, null)]
    [Arguments(PageAssemblyCatalog.CounterPageKey, null)]
    [Arguments(PageAssemblyCatalog.WeatherPageKey, null)]
    [Arguments(PageAssemblyCatalog.FoundationExamplesPageKey, null)]
    [Arguments(PageAssemblyCatalog.IconsPageKey, null)]
    [Arguments(PageAssemblyCatalog.MarkdownExamplesPageKey, "Markdig.dll")]
    [Arguments(PageAssemblyCatalog.DebugPageKey, null)]
    [Arguments(PageAssemblyCatalog.AuthPageKey, null)]
    public async Task Catalog_Integrity_Every_Page_Key_Resolves_And_Home_Prefetch_Keys_Are_Articles_And_Videos(
        string pageKey,
        string? routingContractDll
    )
    {
        await Assert.That(PageAssemblyCatalog.TryGetAssemblies(pageKey, out var dlls)).IsTrue();
        await Assert.That(dlls).IsNotEmpty();
        await Assert.That(dlls.Distinct().Count()).IsEqualTo(dlls.Count);

        if (routingContractDll is not null)
        {
            await Assert.That(dlls).Contains(routingContractDll);
        }

        await Assert.That(PageAssemblyCatalog.HomePrefetchPageKeys.Count).IsEqualTo(2);
        await Assert
            .That(PageAssemblyCatalog.HomePrefetchPageKeys)
            .Contains(PageAssemblyCatalog.ArticlesPageKey);
        await Assert
            .That(PageAssemblyCatalog.HomePrefetchPageKeys)
            .Contains(PageAssemblyCatalog.VideosPageKey);
    }

    [Test]
    public async Task TryGetPageKeyFromPath_Maps_Articles_Videos_And_ApiHealth()
    {
        using (Assert.Multiple())
        {
            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("articles", out var articlesKey))
                .IsTrue();
            await Assert.That(articlesKey).IsEqualTo(PageAssemblyCatalog.ArticlesPageKey);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("/videos?x=1", out var videosKey))
                .IsTrue();
            await Assert.That(videosKey).IsEqualTo(PageAssemblyCatalog.VideosPageKey);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("api-health", out var healthKey))
                .IsTrue();
            await Assert.That(healthKey).IsEqualTo(PageAssemblyCatalog.ApiHealthPageKey);
        }
    }

    [Test]
    public async Task TryGetPageKeyFromPath_Maps_Sample_Pages()
    {
        using (Assert.Multiple())
        {
            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("counter", out var counterKey))
                .IsTrue();
            await Assert.That(counterKey).IsEqualTo(PageAssemblyCatalog.CounterPageKey);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("/weather?x=1", out var weatherKey))
                .IsTrue();
            await Assert.That(weatherKey).IsEqualTo(PageAssemblyCatalog.WeatherPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetPageKeyFromPath(
                        "foundationexamples",
                        out var foundationKey
                    )
                )
                .IsTrue();
            await Assert
                .That(foundationKey)
                .IsEqualTo(PageAssemblyCatalog.FoundationExamplesPageKey);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("icons", out var iconsKey))
                .IsTrue();
            await Assert.That(iconsKey).IsEqualTo(PageAssemblyCatalog.IconsPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetPageKeyFromPath(
                        "markdownexamples",
                        out var markdownKey
                    )
                )
                .IsTrue();
            await Assert.That(markdownKey).IsEqualTo(PageAssemblyCatalog.MarkdownExamplesPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetPageKeyFromPath(
                        "debug/localstorage",
                        out var debugKey
                    )
                )
                .IsTrue();
            await Assert.That(debugKey).IsEqualTo(PageAssemblyCatalog.DebugPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetPageKeyFromPath("debug/resetcache", out var resetKey)
                )
                .IsTrue();
            await Assert.That(resetKey).IsEqualTo(PageAssemblyCatalog.DebugPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetAssemblies(
                        PageAssemblyCatalog.DebugPageKey,
                        out var debugDlls
                    )
                )
                .IsTrue();
            await Assert.That(debugDlls).Contains("Debug.dll");
            await Assert.That(debugDlls.Count).IsEqualTo(1);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("redirect", out var authKey))
                .IsTrue();
            await Assert.That(authKey).IsEqualTo(PageAssemblyCatalog.AuthPageKey);

            await Assert
                .That(
                    PageAssemblyCatalog.TryGetAssemblies(
                        PageAssemblyCatalog.AuthPageKey,
                        out var authDlls
                    )
                )
                .IsTrue();
            await Assert.That(authDlls).Contains("Auth.dll");
            await Assert.That(authDlls.Count).IsEqualTo(1);

            await Assert
                .That(PageAssemblyCatalog.TryGetPageKeyFromPath("unknown-route", out _))
                .IsFalse();
        }
    }
}
