using System.Globalization;
using Bunit;
using redmuffin.Blazor.StaticWeb.Components.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Common.Components;

[Category("Feature:Cache")]
public sealed partial class RefreshBadgeTests
{
    [Test]
    [Arguments(RefreshBadgeState.Visible)]
    [Arguments(RefreshBadgeState.Hidden)]
    [Arguments(RefreshBadgeState.Loading)]
    [Arguments(RefreshBadgeState.Error)]
    public async Task RefreshBadge_Should_Be_Keyboard_Accessible(RefreshBadgeState state)
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, state)
        );

        var button = component.Find("button");

        // Assert
        await Assert.That(button.TagName.ToLowerInvariant()).IsEqualTo("button");
        await Assert.That(button.GetAttribute("title")).IsNotEmpty();

        var tabindex = button.GetAttribute("tabindex");
        var parsedTabindex = tabindex is null
            ? 0
            : int.Parse(tabindex, CultureInfo.InvariantCulture);
        await Assert.That(parsedTabindex).IsGreaterThanOrEqualTo(0);

        var isDisabled = button.HasAttribute("disabled");
        if (state == RefreshBadgeState.Loading)
        {
            await Assert.That(isDisabled).IsTrue();
        }
        else
        {
            await Assert.That(isDisabled).IsFalse();
        }
    }

    [Test]
    [Arguments(RefreshBadgeState.Visible, "fa-sync-alt", null)]
    [Arguments(RefreshBadgeState.Loading, "fa-spinner", "fa-spin")]
    [Arguments(RefreshBadgeState.Error, "fa-exclamation-triangle", null)]
    public async Task RefreshBadge_Should_Have_Appropriate_Icon_With_State_Classes(
        RefreshBadgeState state,
        string stateIconClass,
        string? animationClass
    )
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, state)
        );

        // Assert
        var icons = component.FindAll("i");
        await Assert.That(icons.Count).IsEqualTo(1);

        var iconClass = icons[0].GetAttribute("class");
        await Assert.That(iconClass).Contains("fas");
        await Assert.That(iconClass).Contains(stateIconClass);

        if (animationClass is not null)
        {
            await Assert.That(iconClass).Contains(animationClass);
        }
    }

    [Test]
    [Arguments(RefreshBadgeState.Visible, "refresh")]
    [Arguments(RefreshBadgeState.Loading, "refreshing")]
    [Arguments(RefreshBadgeState.Error, "failed")]
    [Arguments(RefreshBadgeState.Hidden, "refresh")]
    public async Task RefreshBadge_Should_Have_Descriptive_Tooltips_For_All_States(
        RefreshBadgeState state,
        string expectedKeyword
    )
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, state)
        );

        // Assert
        var tooltip = component.Find("button").GetAttribute("title");
        await Assert.That(tooltip).IsNotNull();
        await Assert.That(tooltip).IsNotEmpty();
        await Assert.That(tooltip!.ToLowerInvariant()).Contains(expectedKeyword);
    }

    [Test]
    public async Task RefreshBadge_WithCustomCssClass_AppliesCustomClass()
    {
        // Arrange
        using var scope = CreateTestScope();
        const string customClass = "custom-badge-class";

        // Act
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters
                .Add(p => p.State, RefreshBadgeState.Visible)
                .Add(p => p.CssClass, customClass)
        );

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
        var component = scope.Context.Render<RefreshBadge>(parameters =>
            parameters.Add(p => p.State, RefreshBadgeState.Visible).Add(p => p.Text, customText)
        );

        // Assert
        await Assert.That(component.Find("span").TextContent).IsEqualTo(customText);
    }
}
