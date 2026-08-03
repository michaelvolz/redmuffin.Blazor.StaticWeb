using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed class PageAssemblyCatalogTests
{
    [Test]
    public async Task Articles_And_Videos_List_Page_Components_And_Raindrop_Dlls()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.ArticlesPageKey)).IsTrue();
            await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.VideosPageKey)).IsTrue();

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.ArticlesPageKey, out var articles))
                .IsTrue();
            await Assert.That(articles).Contains("Articles.dll");
            await Assert.That(articles).Contains("Components.dll");
            await Assert.That(articles).Contains("Raindrop.dll");

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.VideosPageKey, out var videos))
                .IsTrue();
            await Assert.That(videos).Contains("Videos.dll");
            await Assert.That(videos).Contains("Components.dll");
            await Assert.That(videos).Contains("Raindrop.dll");
        }
    }

    [Test]
    public async Task Home_Prefetch_Keys_Are_Articles_And_Videos_Only()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.HomePrefetchPageKeys.Count).IsEqualTo(2);
            await Assert.That(PageAssemblyCatalog.HomePrefetchPageKeys)
                .Contains(PageAssemblyCatalog.ArticlesPageKey);
            await Assert.That(PageAssemblyCatalog.HomePrefetchPageKeys)
                .Contains(PageAssemblyCatalog.VideosPageKey);
        }
    }

    [Test]
    public async Task ApiHealth_Lists_Page_And_Module_Dlls()
    {
        await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.ApiHealthPageKey)).IsTrue();
        await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.ApiHealthPageKey, out var names))
            .IsTrue();
        await Assert.That(names).Contains("ApiHealth.Page.dll");
        await Assert.That(names).Contains("AzureHealthCheck.dll");
    }

    [Test]
    public async Task TryGetPageKeyFromPath_Maps_Articles_Videos_And_ApiHealth()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("articles", out var articlesKey))
                .IsTrue();
            await Assert.That(articlesKey).IsEqualTo(PageAssemblyCatalog.ArticlesPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("/videos?x=1", out var videosKey))
                .IsTrue();
            await Assert.That(videosKey).IsEqualTo(PageAssemblyCatalog.VideosPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("api-health", out var healthKey))
                .IsTrue();
            await Assert.That(healthKey).IsEqualTo(PageAssemblyCatalog.ApiHealthPageKey);
        }
    }

    [Test]
    public async Task Sample_Pages_List_Expected_Need_Sets()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.CounterPageKey, out var counter))
                .IsTrue();
            await Assert.That(counter).Contains("Counter.dll");
            await Assert.That(counter.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.WeatherPageKey, out var weather))
                .IsTrue();
            await Assert.That(weather).Contains("Weather.dll");
            await Assert.That(weather.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.FoundationExamplesPageKey, out var foundation))
                .IsTrue();
            await Assert.That(foundation).Contains("FoundationExamples.dll");
            await Assert.That(foundation.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.IconsPageKey, out var icons))
                .IsTrue();
            await Assert.That(icons).Contains("Icons.dll");
            await Assert.That(icons.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.MarkdownExamplesPageKey, out var markdown))
                .IsTrue();
            await Assert.That(markdown).Contains("MarkdownExamples.dll");
            await Assert.That(markdown).Contains("Markdig.dll");
            await Assert.That(markdown.Count).IsEqualTo(2);
        }
    }

    [Test]
    public async Task TryGetPageKeyFromPath_Maps_Sample_Pages()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("counter", out var counterKey))
                .IsTrue();
            await Assert.That(counterKey).IsEqualTo(PageAssemblyCatalog.CounterPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("/weather?x=1", out var weatherKey))
                .IsTrue();
            await Assert.That(weatherKey).IsEqualTo(PageAssemblyCatalog.WeatherPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("foundationexamples", out var foundationKey))
                .IsTrue();
            await Assert.That(foundationKey).IsEqualTo(PageAssemblyCatalog.FoundationExamplesPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("icons", out var iconsKey))
                .IsTrue();
            await Assert.That(iconsKey).IsEqualTo(PageAssemblyCatalog.IconsPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("markdownexamples", out var markdownKey))
                .IsTrue();
            await Assert.That(markdownKey).IsEqualTo(PageAssemblyCatalog.MarkdownExamplesPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("debug/localstorage", out var debugKey))
                .IsTrue();
            await Assert.That(debugKey).IsEqualTo(PageAssemblyCatalog.DebugPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("debug/resetcache", out var resetKey))
                .IsTrue();
            await Assert.That(resetKey).IsEqualTo(PageAssemblyCatalog.DebugPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.DebugPageKey, out var debugDlls))
                .IsTrue();
            await Assert.That(debugDlls).Contains("Debug.dll");
            await Assert.That(debugDlls.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("redirect", out var authKey))
                .IsTrue();
            await Assert.That(authKey).IsEqualTo(PageAssemblyCatalog.AuthPageKey);

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.AuthPageKey, out var authDlls))
                .IsTrue();
            await Assert.That(authDlls).Contains("Auth.dll");
            await Assert.That(authDlls.Count).IsEqualTo(1);

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("unknown-route", out _))
                .IsFalse();
        }
    }
}
