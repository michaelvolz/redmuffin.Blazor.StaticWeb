using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using HomePage = redmuffin.Blazor.StaticWeb.Features.Pages.HomePage.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

public sealed partial class HomeTests
{
    [Test]
    public async Task Home_AdvancedErrorHandling_MixedFailureScenarios()
    {
        // Arrange - Combine multiple failure modes
        using var scope = new TestScope("http://localhost:3000/")
            .WithThrowingNavigation()
            .WithFailingHttpClient();

        // Act
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        // Clear logs and trigger HTTP failure
        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Component should handle multiple simultaneous failure modes
        using (Assert.Multiple())
        {
            // HTTP error during button click should be logged
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Error && entry.Message.Contains("Dummy API call failed"))).IsTrue();

            // Component should still function despite errors
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_AsyncExceptionPropagation_DoesNotCrashComponent()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button.primary-button");

        // Act - Trigger async operation that throws exceptions
        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Component should remain functional despite async exceptions
        using (Assert.Multiple())
        {
            // Exception should be logged, not bubble up
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Error && entry.Message.Contains("Dummy API call failed"))).IsTrue();

            // Component should remain rendered and functional
            await Assert.That(component.Markup).IsNotNull().And.Contains("Click me");

            // Second click should still work
            scope.Logger.LogEntries.Clear();
            await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Button clicked"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_BrowserSecurityPolicies_GracefulDegradation()
    {
        // Arrange - Simulate browser security restrictions (CORS, CSP, etc.)
        using var scope = CreateTestScope();
        scope.BUnitContext.JSInterop.Setup<bool>("window.crypto.getRandomValues")
            .SetException(new JSException("Access denied by security policy"));

        // Act - Component should render despite browser security restrictions
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Component gracefully handles security restrictions
        using (Assert.Multiple())
        {
            // Component renders successfully despite JS restrictions
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Core functionality remains available
            var button = component.Find("button.primary-button");
            await Assert.That(button).IsNotNull();
            await Assert.That(button.TextContent.Trim()).IsEqualTo("Click me");
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_ComponentParameterValidation_HandlesInvalidValues()
    {
        // Arrange - Test component resilience to invalid parameter values
        using var scope = CreateTestScope();

        // Setup invalid cascading parameters
        scope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "invalid-theme-value");
        var invalidPreferences = new Dictionary<string, object>
        {
            ["null-value"] = null!,
            ["empty-string"] = "",
            ["negative-number"] = -1
        };
        scope.BUnitContext.Services.AddCascadingValue<IDictionary<string, object>>("UserPreferences", _ => invalidPreferences);

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Component should handle invalid parameter values gracefully
        using (Assert.Multiple())
        {
            // Component should use default theme for invalid value
            await Assert.That(component.Instance.GetThemeClass()).IsEqualTo("theme-default");

            // Should handle null and invalid preference values gracefully
            await Assert.That(component.Instance.GetUserPreference("null-value")).IsNull();
            await Assert.That(component.Instance.GetUserPreference("empty-string")).IsEqualTo("");
            await Assert.That(component.Instance.GetUserPreference("negative-number")).IsEqualTo(-1);
            await Assert.That(component.Instance.GetUserPreference("nonexistent")).IsNull();

            // Component should still render successfully
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    public async Task Home_Authorization_NullAuthenticationState_HandlesGracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        // Don't provide AuthenticationState cascading value (it will be null)

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify null authentication state is handled gracefully
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.IsAuthenticated).IsFalse();
            await Assert.That(component.Instance.CurrentUserName).IsNull();
            // Component should still render successfully
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    public async Task Home_CascadingParameters_NullUserPreferences_HandlesGracefully()
    {
        // Arrange
        using var scope = CreateTestScope();
        // Don't set UserPreferences cascading value (it will be null)

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify null preferences are handled gracefully
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.UserPreferences).IsNull();
            await Assert.That(component.Instance.GetUserPreference("anyKey")).IsNull();
            // Component should still render successfully
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    public async Task Home_ErrorRecovery_ContinuesAfterHttpFailure()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        // Act - Trigger error, then test recovery
        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        var firstErrorLogged = scope.Logger.LogEntries.Any(entry =>
            entry.LogLevel == LogLevel.Error && entry.Message.Contains("Dummy API call failed"));

        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify error recovery behavior (single recovery concern)
        using (Assert.Multiple())
        {
            await Assert.That(firstErrorLogged).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Error)).IsTrue();
        }
    }

    [Test]
    public async Task Home_ErrorRecovery_ContinuesWorkingAfterHttpClientFailure()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        // Act - Trigger failing operation, then test recovery
        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Verify error was handled
        var errorLogged = scope.Logger.LogEntries.Any(entry =>
            entry.LogLevel == LogLevel.Error && entry.Message.Contains("Dummy API call failed"));
        await Assert.That(errorLogged).IsTrue();

        // Act - Try clicking again to test recovery
        scope.Logger.LogEntries.Clear();
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Component should still work after previous failure
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Chain related assertions on same object
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Should log another error (since we're still using failing HTTP client)
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.LogLevel == LogLevel.Error)).IsTrue();
        }
    }

    [Test]
    public async Task Home_HttpClientFailure_LogsErrorCorrectly()
    {
        // Arrange
        using var scope = CreateFailingHttpTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        scope.Logger.LogEntries.Clear();

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify HTTP error logging (single error logging concern)
        using (Assert.Multiple())
        {
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Dummy API call failed"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.LogLevel == LogLevel.Error)).IsTrue();
        }
    }

    [Test]
    public async Task Home_JSInterop_HandlesThrowingJSCalls_WithoutComponentFailure()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop();

        // Setup JS interop to throw on specific calls to test error handling
        scope.BUnitContext.JSInterop.Setup<string>("window.nonExistentFunction")
            .SetException(new JSException("Function not defined"));

        // Act & Assert - Component should render successfully despite JS errors
        await Assert.That(scope.BUnitContext.Render<HomePage>().Markup).Contains("redmuffin.StaticWeb");
    }

    [Test]
    public async Task Home_JSInterop_HandlesTimeoutScenarios_GracefulDegradation()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop();

        // Setup JS interop calls that simulate timeout behavior
        scope.BUnitContext.JSInterop.Setup<string>("setTimeout").SetResult("timeout_handled");

        var component = scope.BUnitContext.Render<HomePage>();

        // Act & Assert - Component should handle JS timeout scenarios gracefully
        await Assert.That(component.Markup).Contains("Click me");
    }
}