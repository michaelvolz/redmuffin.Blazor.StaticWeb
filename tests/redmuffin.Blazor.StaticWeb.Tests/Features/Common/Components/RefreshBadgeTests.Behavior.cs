using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Components.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Common.Components;

[Category("Feature:Cache")]
public sealed partial class RefreshBadgeTests
{
    [Test]
    public async Task RefreshBadge_DefaultState_RendersHiddenBadge()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>();

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--hidden");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task RefreshBadge_ErrorState_ClickTriggersOnClickEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var clickTriggered = false;
        var onClickCallback = EventCallback.Factory.Create(this, () => clickTriggered = true);

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters
                .Add(p => p.State, RefreshBadgeState.Error)
                .Add(p => p.OnClick, onClickCallback)
        );

        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        await Assert.That(clickTriggered).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_ErrorState_RendersErrorBadge()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, RefreshBadgeState.Error)
        );

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--error");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task RefreshBadge_LoadingState_RendersLoadingBadgeWithDisabled()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, RefreshBadgeState.Loading)
        );

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--loading");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_VisibleState_ClickTriggersOnClickEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var clickTriggered = false;
        var onClickCallback = EventCallback.Factory.Create(this, () => clickTriggered = true);

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters
                .Add(p => p.State, RefreshBadgeState.Visible)
                .Add(p => p.OnClick, onClickCallback)
        );

        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        await Assert.That(clickTriggered).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_VisibleState_RendersVisibleBadge()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, RefreshBadgeState.Visible)
        );

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--visible");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsFalse();
    }
}
