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
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Error)
            .Add(p => p.OnClick, onClickCallback));

        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        await Assert.That(clickTriggered).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_ErrorState_HasCorrectTooltip()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Error));

        // Assert
        await Assert.That(component.Find("button").GetAttribute("title")).IsEqualTo("Refresh failed - click to retry");
    }

    [Test]
    public async Task RefreshBadge_ErrorState_HasErrorIcon()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Error));

        // Assert
        var icon = component.Find("i");
        var iconClass = icon.GetAttribute("class");
        await Assert.That(iconClass).Contains("fas");
        await Assert.That(iconClass).Contains("fa-exclamation-triangle");
    }

    [Test]
    public async Task RefreshBadge_ErrorState_RendersErrorBadge()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Error));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--error");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task RefreshBadge_HiddenState_HasDefaultTooltip()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Hidden));

        // Assert
        await Assert.That(component.Find("button").GetAttribute("title")).IsEqualTo("Refresh");
    }

    [Test]
    public async Task RefreshBadge_LoadingState_HasCorrectTooltip()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Loading));

        // Assert
        await Assert.That(component.Find("button").GetAttribute("title")).IsEqualTo("Refreshing...");
    }

    [Test]
    public async Task RefreshBadge_LoadingState_HasSpinnerIcon()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Loading));

        // Assert
        var icon = component.Find("i");
        var iconClass = icon.GetAttribute("class");
        await Assert.That(iconClass).Contains("fas");
        await Assert.That(iconClass).Contains("fa-spinner");
        await Assert.That(iconClass).Contains("fa-spin");
    }

    [Test]
    public async Task RefreshBadge_LoadingState_RendersLoadingBadgeWithDisabled()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Loading));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--loading");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_Should_Be_Disabled_During_Loading_State()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Loading));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.HasAttribute("disabled")).IsTrue();
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--loading");
    }

    [Test]
    public async Task RefreshBadge_VisibleState_ClickTriggersOnClickEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var clickTriggered = false;
        var onClickCallback = EventCallback.Factory.Create(this, () => clickTriggered = true);

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.OnClick, onClickCallback));

        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert
        await Assert.That(clickTriggered).IsTrue();
    }

    [Test]
    public async Task RefreshBadge_VisibleState_HasCorrectIcon()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible));

        // Assert
        var icon = component.Find("i");
        var iconClass = icon.GetAttribute("class");
        await Assert.That(iconClass).Contains("fas");
        await Assert.That(iconClass).Contains("fa-sync-alt");
    }

    [Test]
    public async Task RefreshBadge_VisibleState_HasCorrectTooltip()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible));

        // Assert
        await Assert.That(component.Find("button").GetAttribute("title")).IsEqualTo("Click to refresh content");
    }

    [Test]
    public async Task RefreshBadge_VisibleState_RendersVisibleBadge()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge--visible");
        await Assert.That(component.Find("button").HasAttribute("disabled")).IsFalse();
    }
}
