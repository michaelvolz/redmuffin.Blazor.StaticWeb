using redmuffin.Blazor.StaticWeb.Core.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public sealed class PageAssemblyCatalogTests
{
    [Test]
    public async Task Articles_And_Videos_List_Page_Module_And_Raindrop_Dlls()
    {
        using (Assert.Multiple())
        {
            await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.ArticlesPageKey)).IsTrue();
            await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.VideosPageKey)).IsTrue();

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.ArticlesPageKey, out var articles))
                .IsTrue();
            await Assert.That(articles).Contains("Articles.dll");
            await Assert.That(articles).Contains("Raindrop.dll");

            await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.VideosPageKey, out var videos))
                .IsTrue();
            await Assert.That(videos).Contains("Videos.dll");
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
    public async Task ApiHealth_Lists_ApiHealth_Dll()
    {
        await Assert.That(PageAssemblyCatalog.HasAssemblies(PageAssemblyCatalog.ApiHealthPageKey)).IsTrue();
        await Assert.That(PageAssemblyCatalog.TryGetAssemblies(PageAssemblyCatalog.ApiHealthPageKey, out var names))
            .IsTrue();
        await Assert.That(names).Contains("ApiHealth.dll");
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

            await Assert.That(PageAssemblyCatalog.TryGetPageKeyFromPath("counter", out _))
                .IsFalse();
        }
    }
}
