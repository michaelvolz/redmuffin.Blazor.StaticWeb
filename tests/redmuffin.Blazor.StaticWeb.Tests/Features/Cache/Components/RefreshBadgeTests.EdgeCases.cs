using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Features.Cache.Components;
using redmuffin.Blazor.StaticWeb.Features.Cache.Enums;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Cache.Components;

[Category("Feature:Cache")]
public sealed partial class RefreshBadgeTests
{
    [Test]
    public async Task RefreshBadge_LoadingState_ClickDoesNotTriggerOnClickEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var clickTriggered = false;
        var onClickCallback = EventCallback.Factory.Create(this, () => clickTriggered = true);

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Loading)
            .Add(p => p.OnClick, onClickCallback));

        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        await Assert.That(clickTriggered).IsFalse();
    }

    [Test]
    public async Task RefreshBadge_WithEmptyText_DoesNotRenderTextSpan()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.Text, string.Empty));

        // Assert
        await Assert.That(component.FindAll("span")).IsEmpty();
    }
}