using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

/// <summary>
///     Integration tests for Articles page caching functionality.
///     Tests the integration between Articles component, RefreshBadge, and caching services.
/// </summary>
[Category("Feature:Articles")]
[Category("Integration")]
public partial class ArticlesPageCacheTests
{
    [Test]
    public async Task ArticlesPage_IdenticalFreshData_DoesNotShowRefreshBadge()
    {
        // Arrange
        using var scope = CreateTestScope();
        var identicalArticles = new List<RaindropItem>
        {
            CreateTestArticle("1", "Same Article", "Same excerpt")
        };

        scope.CacheService_Mock.SetupCachedData("Articles", identicalArticles);
        scope.RaindropAPI_Mock.SetupArticles(identicalArticles); // Same data

        // Act
        var component = scope.Context.Render<Articles>();

        // Assert
        await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
    }
}