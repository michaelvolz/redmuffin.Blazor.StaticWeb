using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using HomePage = redmuffin.Blazor.StaticWeb.Pages.Home.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
public sealed partial class HomeTests
{
    [Test]
    public async Task Home_Accessibility_AlertRegionUpdatesOnFormValidation()
    {
        // Arrange
        using var scope = CreateTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();
        var input = component.Find("input#demo-input");
        await input.ChangeAsync(new ChangeEventArgs { Value = "" }).ConfigureAwait(false);

        // Act
        delayProvider.Arm();
        var submitTask = component.Find("button[type='submit']").ClickAsync(new MouseEventArgs());

        // Assert - the alert region carries the validation message while the handler is in flight
        var alertRegion = component.Find("#alert-region");
        using (Assert.Multiple())
        {
            await Assert.That(alertRegion.TextContent).Contains("Please enter a value");
            await Assert.That(alertRegion.GetAttribute("aria-live")).IsEqualTo("assertive");
            await Assert.That(alertRegion.GetAttribute("aria-atomic")).IsEqualTo("true");
        }

        delayProvider.Release();
        await submitTask.ConfigureAwait(false);
    }

    [Test]
    public async Task Home_Accessibility_StatusRegionUpdatesOnButtonClick()
    {
        // Arrange
        using var scope = CreateTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();

        var statusRegionBeforeClick = component.Find("#status-region").TextContent;

        // Act
        delayProvider.Arm();
        var clickTask = component.Find("button.primary-button").ClickAsync(new MouseEventArgs());

        // Assert - the status region changes to the API result while the handler is in flight
        var statusRegion = component.Find("#status-region");
        using (Assert.Multiple())
        {
            await Assert.That(statusRegionBeforeClick.Trim()).IsEmpty();
            await Assert.That(statusRegion.TextContent).Contains("API call completed");
            await Assert.That(statusRegion.GetAttribute("aria-live")).IsEqualTo("polite");
            await Assert.That(statusRegion.GetAttribute("aria-atomic")).IsEqualTo("true");
        }

        delayProvider.Release();
        await clickTask.ConfigureAwait(false);
    }

    [Test]
    public async Task Home_AdvancedScenarios_RapidStateChanges_MaintainDataIntegrity()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var submitButton = component.Find("button[type='submit']");
        var input = component.Find("input#demo-input");

        // Act - Rapid successive state changes to test race conditions
        await input.ChangeAsync(new ChangeEventArgs { Value = "test1" }).ConfigureAwait(false);
        await input.ChangeAsync(new ChangeEventArgs { Value = "test2" }).ConfigureAwait(false);
        await input.ChangeAsync(new ChangeEventArgs { Value = "final" }).ConfigureAwait(false);
        await submitButton.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Final state should be consistent despite rapid changes
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.DemoInputValue).IsEqualTo(string.Empty); // Cleared after submit
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Message.Contains("Form submitted") && entry.Message.Contains("final")
                    )
                )
                .IsTrue();
        }
    }

    [Test]
    public async Task Home_ConcurrentClicks_Complete_And_Component_Stays_Interactive()
    {
        // Arrange
        using var scope = CreateTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();

        // Act - fire several clicks without awaiting, then await completion
        var button = component.Find("button.primary-button");
        var tasks = new List<Task>();
        for (var i = 0; i < 3; i++)
            tasks.Add(button.ClickAsync(new MouseEventArgs()));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // A further click still renders the status message
        button = component.Find("button.primary-button");
        delayProvider.Arm();
        var followUpClick = button.ClickAsync(new MouseEventArgs());
        await Assert
            .That(component.Find("#status-region").TextContent)
            .Contains("API call completed");
        delayProvider.Release();
        await followUpClick.ConfigureAwait(false);

        // Assert - concurrent handling left no error-level entries behind
        await Assert
            .That(scope.Logger.LogEntries.Any(entry => entry.Level == LogLevel.Error))
            .IsFalse();
    }
}
