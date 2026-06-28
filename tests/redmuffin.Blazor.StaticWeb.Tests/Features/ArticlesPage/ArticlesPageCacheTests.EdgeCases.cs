using Bunit;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

[Category("Feature:Articles")]
public sealed partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_CacheFailure_FallsBackToFreshData()
    {
        // Arrange
        using var scope = CreateTestScope();
        var freshArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Fallback Article", "Fallback excerpt")
        };

        scope.CacheService_Mock.SetupCacheFailure("Articles");
        scope.RaindropAPI_Mock.SetupArticles(freshArticles);

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