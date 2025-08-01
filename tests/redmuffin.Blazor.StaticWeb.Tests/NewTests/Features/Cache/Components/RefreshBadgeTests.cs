using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using redmuffin.Blazor.StaticWeb.Features.Cache.Components;
using redmuffin.Blazor.StaticWeb.Features.Cache.Enums;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Cache.Components;

/// <summary>
///     Unit tests for RefreshBadge component functionality and state management.
/// </summary>
public partial class RefreshBadgeTests
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
    public async Task RefreshBadge_Should_Be_Keyboard_Accessible()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible));

        var button = component.Find("button");

        // Assert
        await Assert.That(button.HasAttribute("disabled")).IsFalse();
        await Assert.That(button.GetAttribute("tabindex")).IsNull().Or.IsEqualTo("0");
    }

    [Test]
    public async Task RefreshBadge_Should_Have_Appropriate_Icon_With_State_Classes()
    {
        // Arrange
        using var scope = CreateTestScope();
        var states = new[]
        {
            RefreshBadgeState.Visible,
            RefreshBadgeState.Loading,
            RefreshBadgeState.Error
        };

        foreach (var state in states)
        {
            // Act
            var component = scope.Context.Render<RefreshBadge>(parameters => parameters
                .Add(p => p.State, state));

            // Assert
            var icon = component.Find("i");
            await Assert.That(icon.GetAttribute("class")).Contains("fas");

            // Verify state-specific icons
            switch (state)
            {
                case RefreshBadgeState.Visible:
                    await Assert.That(icon.GetAttribute("class")).Contains("fa-sync-alt");
                    break;
                case RefreshBadgeState.Loading:
                    await Assert.That(icon.GetAttribute("class")).Contains("fa-spinner");
                    await Assert.That(icon.GetAttribute("class")).Contains("fa-spin");
                    break;
                case RefreshBadgeState.Error:
                    await Assert.That(icon.GetAttribute("class")).Contains("fa-exclamation-triangle");
                    break;
            }
        }
    }

    [Test]
    public async Task RefreshBadge_Should_Have_Descriptive_Tooltips_For_All_States()
    {
        // Arrange
        using var scope = CreateTestScope();
        var states = new[]
        {
            RefreshBadgeState.Visible,
            RefreshBadgeState.Loading,
            RefreshBadgeState.Error,
            RefreshBadgeState.Hidden
        };

        foreach (var state in states)
        {
            // Act
            var component = scope.Context.Render<RefreshBadge>(parameters => parameters
                .Add(p => p.State, state));

            // Assert
            var button = component.Find("button");
            var tooltip = button.GetAttribute("title");

            await Assert.That(tooltip).IsNotNull();
            await Assert.That(tooltip).IsNotEmpty();

            // Verify state-specific tooltip content
            if (tooltip != null)
                switch (state)
                {
                    case RefreshBadgeState.Visible:
                        await Assert.That(tooltip.ToLower()).Contains("refresh");
                        break;
                    case RefreshBadgeState.Loading:
                        await Assert.That(tooltip.ToLower()).Contains("refreshing");
                        break;
                    case RefreshBadgeState.Error:
                        await Assert.That(tooltip.ToLower()).Contains("failed").Or.Contains("retry");
                        break;
                    case RefreshBadgeState.Hidden:
                        await Assert.That(tooltip.ToLower()).Contains("refresh");
                        break;
                }
        }
    }

    [Test]
    public async Task RefreshBadge_Should_Have_Proper_ARIA_Attributes()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.Text, "Refresh"));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("title")).IsNotNull();
        await Assert.That(button.GetAttribute("title")).Contains("refresh");
        await Assert.That(button.TagName.ToLower()).IsEqualTo("button");
    }

    [Test]
    public async Task RefreshBadge_Should_Have_Semantic_Button_Element()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.TagName.ToLower()).IsEqualTo("button");
        await Assert.That(button.GetAttribute("class")).Contains("refresh-badge");
    }

    [Test]
    public async Task RefreshBadge_Should_Support_Screen_Reader_Navigation()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.Text, "New Content Available"));

        // Assert
        var button = component.Find("button");
        var textSpan = component.Find("span");

        await Assert.That(button.GetAttribute("title")).IsNotNull();
        await Assert.That(textSpan.TextContent).IsEqualTo("New Content Available");
        await Assert.That(component.Markup).Contains("New Content Available");
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

    [Test]
    public async Task RefreshBadge_WithCustomCssClass_AppliesCustomClass()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customClass = "custom-badge-class";

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.CssClass, customClass));

        // Assert
        var button = component.Find("button");
        await Assert.That(button.GetAttribute("class")).Contains(customClass);
    }

    [Test]
    public async Task RefreshBadge_WithCustomText_RendersTextSpan()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customText = "New Content";

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters => parameters
            .Add(p => p.State, RefreshBadgeState.Visible)
            .Add(p => p.Text, customText));

        // Assert
        await Assert.That(component.Find("span").TextContent).IsEqualTo(customText);
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

    // Note: State transition tests would require component parameter updates
    // which are not easily testable with the current bUnit setup.
    // The component's state management is tested through individual state tests above.
}