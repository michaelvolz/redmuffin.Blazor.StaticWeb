using Bunit;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.ArticlesPage;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.ArticlesPage;

/// <summary>
///     Integration tests for Articles page caching functionality.
///     Tests the integration between Articles component, RefreshBadge, and Mediator load/refresh.
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

        scope.Mediator_Mock.SetupLoad(identicalArticles, isFromCache: true);
        scope.Mediator_Mock.SetupRefresh(identicalArticles);

        // Act
        var component = scope.Context.Render<Articles>();

        if (component.Instance.BackgroundRefreshTask is { } refreshTask)
            await refreshTask.ConfigureAwait(false);

        // Assert
        await Assert.That(component.FindAll(".refresh-badge")).IsEmpty();
    }
}
