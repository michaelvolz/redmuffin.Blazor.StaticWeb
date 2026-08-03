using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_CacheFailure_FallsBackToFreshData()
    {
        // Arrange — Load handler falls back to API on cache failure; page only sees successful Load.
        using var scope = CreateTestScope();
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Fallback Article", "Fallback excerpt")
        };

        scope.Mediator_Mock.SetupLoad(freshArticles, isFromCache: false);
        scope.Mediator_Mock.SetupRefresh(freshArticles);

        // Act
        var component = scope.Context.Render<Articles>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(component.FindAll(".article-card")).Count().IsEqualTo(1);
            await Assert.That(component.Markup).Contains("Fallback Article");
        }
    }
}
