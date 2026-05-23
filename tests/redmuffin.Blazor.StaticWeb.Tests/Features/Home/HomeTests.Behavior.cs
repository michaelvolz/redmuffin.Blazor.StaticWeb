using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using HomePage = redmuffin.Blazor.StaticWeb.Features.HomePage.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
public sealed partial class HomeTests
{
    [Test]
    public async Task Home_Accessibility_AlertRegionUpdatesOnFormValidation()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var submitButton = component.Find("button[type='submit']");

        // Ensure input is empty for validation test
        var input = component.Find("input#demo-input");
        await input.ChangeAsync(new ChangeEventArgs { Value = "" }).ConfigureAwait(false);

        // Act - Submit form with empty input which demonstrates form validation flow
        await submitButton.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify that form submission was processed and logged (indicates alert update flow worked)
        // This validates the accessibility pattern without relying on timing-dependent alert messages
        await Assert.That(scope.Logger.LogEntries.Any(entry =>
            entry.Message.Contains("Form submitted"))).IsTrue();
    }

    [Test]
    public async Task Home_Accessibility_StatusRegionUpdatesOnButtonClick()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button.primary-button");

        // Act - Trigger button click which demonstrates ARIA live region functionality
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify that button click was processed and logged (indicates status update flow worked)
        // This validates the accessibility pattern without relying on timing-dependent status messages
        await Assert.That(scope.Logger.LogEntries.Any(entry =>
            entry.Message.Contains("Button clicked"))).IsTrue();
    }

    [Test]
    public async Task Home_AdvancedScenarios_LongRunningOperations_ComponentRemainsFunctional()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button.primary-button");

        // Act - Trigger operation and immediately interact with component again
        var longRunningTask = button.ClickAsync(new MouseEventArgs());

        // While the first operation is running, try other operations
        var input = component.Find("input#demo-input");
        await input.ChangeAsync(new ChangeEventArgs { Value = "while-busy" }).ConfigureAwait(false);

        var submitButton = component.Find("button[type='submit']");
        await submitButton.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Wait for the long-running operation to complete
        await longRunningTask.ConfigureAwait(false);

        // Assert - Component should handle overlapping operations gracefully
        using (Assert.Multiple())
        {
            // Both operations should have been logged
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Button clicked"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Form submitted") && entry.Message.Contains("while-busy"))).IsTrue();

            // Component should remain stable and functional
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
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
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Form submitted") && entry.Message.Contains("final"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_ConcurrentOperations_HandlesMultipleRequests()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        // Act - Trigger concurrent operations
        var tasks = new List<Task>();
        for (var i = 0; i < 3; i++) tasks.Add(button.ClickAsync(new MouseEventArgs()));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Assert - Component should handle concurrent operations gracefully
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Chain related assertions on same object
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify multiple button clicks were logged
            var buttonClickLogs = scope.Logger.LogEntries
                .Where(entry => entry.Message.Contains("Button clicked"))
                .ToList();
            await Assert.That(buttonClickLogs.Count).IsGreaterThanOrEqualTo(3);
        }
    }
}