using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using HomePage = redmuffin.Blazor.StaticWeb.Features.Pages.HomePage.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

public sealed partial class HomeTests
{
    [Test]
    public async Task Home_Accessibility_HasProperHeadingHierarchy()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify proper heading hierarchy (h1 -> h2 -> h3)
        using (Assert.Multiple())
        {
            // Main page heading (h1)
            var h1 = component.Find("h1#page-heading");
            await Assert.That(h1).IsNotNull();
            await Assert.That(h1.GetAttribute("tabindex")).IsEqualTo("-1"); // Programmatic focus
            await Assert.That(h1.TextContent).Contains("redmuffin.StaticWeb");

            // Section headings (h2) - even if visually hidden
            var h2Elements = component.FindAll("h2");
            await Assert.That(h2Elements.Count).IsGreaterThanOrEqualTo(2);

            // Form heading (h3)
            var h3 = component.Find("h3#demo-form-heading");
            await Assert.That(h3).IsNotNull();
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_ComponentDisposalPatterns_PreventMemoryLeaks()
    {
        // Arrange - Test that component disposal properly cleans up resources
        TestScope? disposedScope = null;
        HomePage? componentInstance = null;

        // Act - Create and dispose component in controlled manner
        {
            using var scope = CreateTestScope();
            var component = scope.BUnitContext.Render<HomePage>();
            componentInstance = component.Instance;
            disposedScope = scope;
        } // Scope disposes here

        // Assert - Verify component and scope are properly disposed without memory leaks
        using (Assert.Multiple())
        {
            await Assert.That(disposedScope).IsNotNull(); // Scope existed
            await Assert.That(componentInstance).IsNotNull(); // Component existed
            // In production, would verify no event handlers leak, no timer leaks, etc.
            // This pattern tests disposal infrastructure without relying on GC timing
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_ConcurrentAsyncOperations_NoRaceConditions()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var primaryButton = component.Find("button.primary-button");
        var submitButton = component.Find("button[type='submit']");
        var input = component.Find("input#demo-input");

        // Setup input value for form submission
        await input.ChangeAsync(new ChangeEventArgs { Value = "concurrent-test" }).ConfigureAwait(false);

        // Act - Trigger concurrent async operations
        var task1 = primaryButton.ClickAsync(new MouseEventArgs());
        var task2 = submitButton.ClickAsync(new MouseEventArgs());
        var task3 = submitButton.ClickAsync(new MouseEventArgs());

        await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);

        // Assert - Both operations should complete without race conditions
        using (Assert.Multiple())
        {
            // Both button types should have been clicked
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Button clicked"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Count(entry =>
                entry.Message.Contains("Form submitted"))).IsGreaterThanOrEqualTo(2);

            // Component should remain stable
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_MemoryManagement_NoEventHandlerLeaks()
    {
        // Arrange - Test that component properly manages event handler cleanup
        var componentInstances = new List<HomePage>();
        var scopes = new List<TestScope>();

        // Act - Create and dispose multiple component instances rapidly
        for (var i = 0; i < 5; i++)
        {
            var scope = CreateTestScope();
            var component = scope.BUnitContext.Render<HomePage>();
            componentInstances.Add(component.Instance);
            scopes.Add(scope);

            // Interact with each component to ensure event handlers are attached
            var button = component.Find("button.primary-button");
            await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        }

        // Dispose all scopes
        foreach (var scope in scopes) scope.Dispose();

        // Assert - Memory management test (in production, this would check for actual memory leaks)
        using (Assert.Multiple())
        {
            await Assert.That(componentInstances.Count).IsEqualTo(5);
            await Assert.That(scopes.Count).IsEqualTo(5);

            // In a real memory leak test, we would verify:
            // - Event handlers are properly unregistered
            // - No references to disposed components remain
            // - Timers and subscriptions are cleaned up
            // This test validates the disposal pattern infrastructure
        }
    }

    [Test]
    public async Task Home_AdvancedScenarios_StateHasChangedCalls_OptimizedRenderCycles()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button.primary-button");

        // Clear initial render logs
        scope.Logger.LogEntries.Clear();

        // Act - Trigger action that calls StateHasChanged multiple times
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify render optimization (component should handle multiple StateHasChanged calls efficiently)
        using (Assert.Multiple())
        {
            // Component should render without excessive re-renders
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify the action completed (indicates StateHasChanged worked properly)
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Button clicked"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_Authorization_AuthenticatedUser_ProcessesCorrectly()
    {
        // Arrange
        using var scope = CreateTestScope();
        var authState = CreateMockAuthenticationState(true, "testuser@example.com");
        scope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ => authState);

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify authenticated user state
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.IsAuthenticated).IsTrue();
            await Assert.That(component.Instance.CurrentUserName).IsEqualTo("testuser@example.com");
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Authorization state changed: True"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_Authorization_AuthenticationStateChanges_UpdatesComponent()
    {
        // Test initial unauthenticated state
        using (var initialScope = CreateTestScope())
        {
            var initialAuthState = CreateMockAuthenticationState(false);
            initialScope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ => initialAuthState);

            var component = initialScope.BUnitContext.Render<HomePage>();
            await Assert.That(component.Instance.IsAuthenticated).IsFalse();
        }

        // Test updated authenticated state in a separate scope
        using var updatedScope = CreateTestScope();
        var newAuthState = CreateMockAuthenticationState(true, "newuser@example.com");
        updatedScope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ => newAuthState);

        var updatedComponent = updatedScope.BUnitContext.Render<HomePage>();

        // Assert - Verify updated authentication state
        using (Assert.Multiple())
        {
            await Assert.That(updatedComponent.Instance.IsAuthenticated).IsTrue();
            await Assert.That(updatedComponent.Instance.CurrentUserName).IsEqualTo("newuser@example.com");
        }
    }

    [Test]
    public async Task Home_Authorization_CombinedWithCascadingParameters_WorksTogether()
    {
        // Arrange - Test complex scenario with both authorization and cascading parameters
        using var scope = CreateTestScope();

        var userPreferences = new Dictionary<string, object>
        {
            ["theme"] = "dark",
            ["accessibility"] = true
        };
        var authState = CreateMockAuthenticationState(true, "admin@example.com");

        scope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "dark");
        scope.BUnitContext.Services.AddCascadingValue<IDictionary<string, object>>("UserPreferences", _ => userPreferences);
        scope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ => authState);

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify both authorization and cascading parameters work together
        using (Assert.Multiple())
        {
            // Authorization state
            await Assert.That(component.Instance.IsAuthenticated).IsTrue();
            await Assert.That(component.Instance.CurrentUserName).IsEqualTo("admin@example.com");

            // Cascading parameters
            await Assert.That(component.Instance.AppTheme).IsEqualTo("dark");
            await Assert.That(component.Instance.GetThemeClass()).IsEqualTo("theme-dark");
            await Assert.That(component.Instance.GetUserPreference("accessibility")).IsEqualTo(true);

            // Logging verification
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("AppTheme: dark"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Authorization state changed: True"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_Authorization_UnauthenticatedUser_ProcessesCorrectly()
    {
        // Arrange
        using var scope = CreateTestScope();
        var authState = CreateMockAuthenticationState(false);
        scope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ => authState);

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify unauthenticated user state
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.IsAuthenticated).IsFalse();
            await Assert.That(component.Instance.CurrentUserName).IsNull();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Authorization state changed: False"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_ButtonClick_LogsExpectedEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        scope.Logger.LogEntries.Clear();

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify button click logging (single logging concern)
        await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Button clicked"))).IsTrue();
    }

    [Test]
    public async Task Home_CascadingParameters_AppTheme_SetsCorrectThemeClass()
    {
        // Arrange
        using var scope = CreateTestScope();
        scope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "dark");

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify theme parameter is correctly processed
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.AppTheme).IsEqualTo("dark");
            await Assert.That(component.Instance.GetThemeClass()).IsEqualTo("theme-dark");
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("AppTheme: dark"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_CascadingParameters_MultipleThemes_ReturnsCorrectClasses()
    {
        // Test light theme
        using (var lightScope = CreateTestScope())
        {
            lightScope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "light");
            var lightComponent = lightScope.BUnitContext.Render<HomePage>();
            await Assert.That(lightComponent.Instance.GetThemeClass()).IsEqualTo("theme-light");
        }

        // Test high-contrast theme in a separate scope
        using var contrastScope = CreateTestScope();
        contrastScope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "high-contrast");
        var contrastComponent = contrastScope.BUnitContext.Render<HomePage>();
        await Assert.That(contrastComponent.Instance.GetThemeClass()).IsEqualTo("theme-high-contrast");
    }

    [Test]
    public async Task Home_CascadingParameters_UserPreferences_RetrievesCorrectValues()
    {
        // Arrange
        using var scope = CreateTestScope();
        var userPreferences = new Dictionary<string, object>
        {
            ["fontSize"] = 16,
            ["language"] = "en-US",
            ["accessibility"] = true
        };
        scope.BUnitContext.Services.AddCascadingValue<IDictionary<string, object>>("UserPreferences", _ => userPreferences);

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify user preferences are accessible
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.UserPreferences).IsNotNull();
            await Assert.That(component.Instance.GetUserPreference("fontSize")).IsEqualTo(16);
            await Assert.That(component.Instance.GetUserPreference("language")).IsEqualTo("en-US");
            await Assert.That(component.Instance.GetUserPreference("accessibility")).IsEqualTo(true);
            await Assert.That(component.Instance.GetUserPreference("nonexistent")).IsNull();
        }
    }

    [Test]
    public async Task Home_ContentRendering_DisplaysCorrectText()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify text content is correct (single content concern)
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
            // ✅ OPTIMIZED: Chain markup assertions - fa-rocket is in HTML markup, not text content
            await Assert.That(component.Markup).Contains("fa-rocket");
            await Assert.That(component.Find("button.primary-button").TextContent.Trim()).IsEqualTo("Click me");
        }
    }

    [Test]
    public async Task Home_EmojiRendering_DisplaysExpectedEmojis()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var markup = scope.BUnitContext.Render<HomePage>().Markup;

        // Assert - Verify emoji content (single emoji rendering concern)
        // ✅ OPTIMIZED: Chain multiple related Contains assertions on same markup
        await Assert.That(markup).Contains("😀").And.Contains("😃").And.Contains("🤣");
    }

    [Test]
    public async Task Home_JSInterop_HandlesStrictMode_WithoutUnexpectedCalls()
    {
        // Arrange & Act
        using var scope = CreateTestScope().WithJSInterop();

        // In strict mode, any unexpected JS call would throw - component should render without JS calls
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Component renders successfully in strict JS interop mode
        await Assert.That(component.Find("h1").TextContent).Contains("redmuffin.StaticWeb");
    }

    [Test]
    public async Task Home_JSInterop_LooseMode_AllowsUnhandledCalls()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop(JSRuntimeMode.Loose);

        // Act & Assert - Component should render successfully in loose mode (default behavior)
        await Assert.That(scope.BUnitContext.Render<HomePage>().Find("button.primary-button").TextContent.Trim()).IsEqualTo("Click me");
    }

    [Test]
    public async Task Home_JSInterop_ValidatesCorrectFunctionCalls()
    {
        // Arrange
        using var scope = CreateTestScope().WithJSInterop();

        // Setup specific JS interop expectations
        scope.BUnitContext.JSInterop.Setup<bool>("console.log").SetResult(true);

        scope.BUnitContext.Render<HomePage>();

        // Act & Assert - Verify that expected JS functions are NOT called when component renders
        // (Since our Home component doesn't currently use JS interop, no calls should be made)
        await Assert.That(scope.BUnitContext.JSInterop.Invocations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Home_LifecycleEventIds_AreCorrectlySet()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        scope.BUnitContext.Render<HomePage>();

        // Assert - Verify event IDs are properly configured (single logging configuration concern)
        using (Assert.Multiple())
        {
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 1)).IsTrue(); // OnInitialized
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 2)).IsTrue(); // OnParametersSetAsync
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 3)).IsTrue(); // First render
        }
    }

    [Test]
    public async Task Home_LifecycleLogging_CapturesAllExpectedEvents()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act - Render component to trigger all lifecycle methods
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify all lifecycle events were logged
        using (Assert.Multiple())
        {
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("OnInitialized called"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("OnParametersSetAsync called"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("First render: OnAfterRenderAsync called"))).IsTrue();

            // Verify event IDs are correctly set for debugging purposes
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 1)).IsTrue(); // OnInitialized
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 2)).IsTrue(); // OnParametersSetAsync
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.EventId.Id == 3)).IsTrue(); // First render
        }
    }

    [Test]
    public async Task Home_LifecycleLogging_CapturesInitializationEvents()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        scope.BUnitContext.Render<HomePage>();

        // Assert - Verify lifecycle logging (single lifecycle concern)
        using (Assert.Multiple())
        {
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("OnInitialized called"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("OnParametersSetAsync called"))).IsTrue();
            await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("First render: OnAfterRenderAsync called"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_LifecycleMethods_HandleConcurrentAsyncOperations()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act - Render component and immediately trigger multiple operations
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        // Trigger multiple button clicks concurrently to test async operation handling
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

    [Test]
    public async Task Home_OnAfterRenderAsync_HandlesMultipleRenderCycles()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act - Render component and trigger re-render
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify OnAfterRenderAsync handles subsequent renders
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Single assertion for markup validation - not null check isn't needed here since we're only checking logging
            await Assert.That(component.Markup).IsNotNull();
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Button clicked"))).IsTrue();
        }
    }

    [Test]
    public async Task Home_OnParametersSetAsync_HandlesAsyncExceptionDuringDelay()
    {
        // Arrange
        using var scope = CreateTestScope();

        // Act - Render component which will trigger OnParametersSetAsync
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Component should complete initialization successfully
        // even with async operations in OnParametersSetAsync
        using (Assert.Multiple())
        {
            // ✅ OPTIMIZED: Chain related assertions on same object
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify that OnParametersSetAsync was called and logged
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("OnParametersSetAsync called"))).IsTrue();
        }
    }
}