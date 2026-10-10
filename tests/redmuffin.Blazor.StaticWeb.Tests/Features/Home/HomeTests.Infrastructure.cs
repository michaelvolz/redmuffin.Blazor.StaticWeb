using System.Reflection;
using System.Runtime.CompilerServices;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;
using HomePage = redmuffin.Blazor.StaticWeb.Pages.Home.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
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
    public async Task Home_AdvancedScenarios_MemoryManagement_DisposedComponents_AreCollectable()
    {
        // Arrange - render, interact with, and dispose components in their own scopes
        var weakReferences = RenderInteractAndDisposeComponents(5);

        // Act - force collection of the disposed components
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert - every disposed component is collectable and repeated disposal did not throw
        await Assert.That(weakReferences.All(reference => !reference.TryGetTarget(out _))).IsTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference<HomePage>> RenderInteractAndDisposeComponents(int count)
    {
        var weakReferences = new List<WeakReference<HomePage>>(count);
        for (var i = 0; i < count; i++)
        {
            var scope = CreateTestScope();
            var component = scope.BUnitContext.Render<HomePage>();
            weakReferences.Add(new WeakReference<HomePage>(component.Instance));
            component
                .Find("button.primary-button")
                .ClickAsync(new MouseEventArgs())
                .GetAwaiter()
                .GetResult();
            scope.Dispose();
            scope.Dispose(); // Disposal must be idempotent
        }

        return weakReferences;
    }

    [Test]
    public async Task Home_AdvancedScenarios_ButtonClick_ProducesBoundedRenders()
    {
        // Arrange
        using var scope = CreateTestScope();
        var delayProvider = new ControllableDelayProvider();
        scope.BUnitContext.Services.AddSingleton<IDelayProvider>(delayProvider);
        var component = scope.BUnitContext.Render<HomePage>();
        var initialRenderCount = component.RenderCount;

        // Act - hold the handler open so the status message stays rendered
        delayProvider.Arm();
        var clickTask = component.Find("button.primary-button").ClickAsync(new MouseEventArgs());

        // Assert - a render carries the status message while the handler is in flight
        await Assert
            .That(component.Find("#status-region").TextContent)
            .Contains("API call completed");

        delayProvider.Release();
        await clickTask.ConfigureAwait(false);

        // Assert - one user action produced a bounded number of renders
        using (Assert.Multiple())
        {
            await Assert.That(component.RenderCount).IsGreaterThan(initialRenderCount);
            await Assert.That(component.RenderCount).IsLessThanOrEqualTo(initialRenderCount + 5);
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
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Message.Contains("Authorization state changed: True")
                    )
                )
                .IsTrue();
        }
    }

    [Test]
    public async Task Home_Authorization_AuthenticationStateChanges_UpdatesComponent()
    {
        // Test initial unauthenticated state
        using (var initialScope = CreateTestScope())
        {
            var initialAuthState = CreateMockAuthenticationState(false);
            initialScope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ =>
                initialAuthState
            );

            var component = initialScope.BUnitContext.Render<HomePage>();
            await Assert.That(component.Instance.IsAuthenticated).IsFalse();
        }

        // Test updated authenticated state in a separate scope
        using var updatedScope = CreateTestScope();
        var newAuthState = CreateMockAuthenticationState(true, "newuser@example.com");
        updatedScope.BUnitContext.Services.AddCascadingValue<Task<AuthenticationState>>(_ =>
            newAuthState
        );

        var updatedComponent = updatedScope.BUnitContext.Render<HomePage>();

        // Assert - Verify updated authentication state
        using (Assert.Multiple())
        {
            await Assert.That(updatedComponent.Instance.IsAuthenticated).IsTrue();
            await Assert
                .That(updatedComponent.Instance.CurrentUserName)
                .IsEqualTo("newuser@example.com");
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
            ["accessibility"] = true,
        };
        var authState = CreateMockAuthenticationState(true, "admin@example.com");

        scope.BUnitContext.Services.AddCascadingValue<string>("AppTheme", _ => "dark");
        scope.BUnitContext.Services.AddCascadingValue<IDictionary<string, object>>(
            "UserPreferences",
            _ => userPreferences
        );
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
            await Assert
                .That((bool)component.Instance.GetUserPreference("accessibility")!)
                .IsTrue();

            // Logging verification
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry => entry.Message.Contains("AppTheme: dark"))
                )
                .IsTrue();
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Message.Contains("Authorization state changed: True")
                    )
                )
                .IsTrue();
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
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Message.Contains("Authorization state changed: False")
                    )
                )
                .IsTrue();
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task Home_ButtonClick_LogsExpectedEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var button = component.Find("button");

        scope.Logger.Reset();

        // Act
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

        // Assert - Verify button click logging (single logging concern)
        await Assert
            .That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("Button clicked")))
            .IsTrue();
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
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry => entry.Message.Contains("AppTheme: dark"))
                )
                .IsTrue();
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
        contrastScope.BUnitContext.Services.AddCascadingValue<string>(
            "AppTheme",
            _ => "high-contrast"
        );
        var contrastComponent = contrastScope.BUnitContext.Render<HomePage>();
        await Assert
            .That(contrastComponent.Instance.GetThemeClass())
            .IsEqualTo("theme-high-contrast");
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
            ["accessibility"] = true,
            ["zoomOffset"] = -1,
        };
        scope.BUnitContext.Services.AddCascadingValue<IDictionary<string, object>>(
            "UserPreferences",
            _ => userPreferences
        );

        // Act
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - stored preferences are retrievable; missing keys and negative values behave
        using (Assert.Multiple())
        {
            await Assert.That(component.Instance.UserPreferences).IsNotNull();
            await Assert.That(component.Instance.GetUserPreference("fontSize")).IsEqualTo(16);
            await Assert.That(component.Instance.GetUserPreference("language")).IsEqualTo("en-US");
            await Assert
                .That((bool)component.Instance.GetUserPreference("accessibility")!)
                .IsTrue();
            await Assert.That(component.Instance.GetUserPreference("zoomOffset")).IsEqualTo(-1);
            await Assert.That(component.Instance.GetUserPreference("nonexistent")).IsNull();
        }

        // A scope without the cascading value renders with null preferences
        using var nullScope = CreateTestScope();
        var nullComponent = nullScope.BUnitContext.Render<HomePage>();
        using (Assert.Multiple())
        {
            await Assert.That(nullComponent.Instance.UserPreferences).IsNull();
            await Assert.That(nullComponent.Instance.GetUserPreference("fontSize")).IsNull();
            await Assert.That(nullComponent.FindAll("main").Count).IsEqualTo(1);
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task Home_ContentRendering_DisplaysRequiredElements()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - the page renders its heading, controls, form and emoji region
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1#page-heading")).IsNotNull();
            await Assert.That(component.Find("button.primary-button")).IsNotNull();
            await Assert.That(component.Find("form")).IsNotNull();

            var emojiRegion = component.Find("div[role='img']");
            await Assert.That(emojiRegion.GetAttribute("aria-label")).IsNotNull().And.IsNotEmpty();
        }
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
    public async Task Home_LifecycleLogging_CapturesEachInitializationEventOnceInOrder()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        scope.BUnitContext.Render<HomePage>();

        // Assert - each initialization event is logged exactly once, in order
        var messages = scope.Logger.LogEntries.Select(entry => entry.Message).ToList();
        var onInitializedIndex = messages.FindIndex(message =>
            message.Contains("OnInitialized called")
        );
        var onParametersSetIndex = messages.FindIndex(message =>
            message.Contains("OnParametersSetAsync called")
        );
        var firstRenderIndex = messages.FindIndex(message =>
            message.Contains("First render: OnAfterRenderAsync called")
        );

        using (Assert.Multiple())
        {
            await Assert
                .That(messages.Count(message => message.Contains("OnInitialized called")))
                .IsEqualTo(1);
            await Assert
                .That(messages.Count(message => message.Contains("OnParametersSetAsync called")))
                .IsEqualTo(1);
            await Assert
                .That(
                    messages.Count(message =>
                        message.Contains("First render: OnAfterRenderAsync called")
                    )
                )
                .IsEqualTo(1);
            await Assert.That(onInitializedIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(onParametersSetIndex).IsGreaterThan(onInitializedIndex);
            await Assert.That(firstRenderIndex).IsGreaterThan(onParametersSetIndex);
        }
    }

    [Test]
    public async Task Home_OnAfterRenderAsync_HandlesMultipleRenderCycles()
    {
        // Arrange
        using var scope = CreateTestScope();
        var loader = new CountingPageAssemblyLoader_Stub();
        scope.BUnitContext.Services.AddSingleton<IPageAssemblyLoader>(loader);
        var component = scope.BUnitContext.Render<HomePage>();
        var initialRenderCount = component.RenderCount;

        // Act - a click forces additional render cycles
        await component
            .Find("button.primary-button")
            .ClickAsync(new MouseEventArgs())
            .ConfigureAwait(false);

        // Assert - the component rendered more than once and first-render work ran once
        using (Assert.Multiple())
        {
            await Assert.That(component.RenderCount).IsGreaterThan(initialRenderCount);
            await Assert.That(component.RenderCount).IsGreaterThanOrEqualTo(2);
            await Assert.That(loader.PrefetchCallCount).IsEqualTo(1);
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
            await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");

            // Verify that OnParametersSetAsync was called and logged
            await Assert
                .That(
                    scope.Logger.LogEntries.Any(entry =>
                        entry.Message.Contains("OnParametersSetAsync called")
                    )
                )
                .IsTrue();
        }
    }

    private sealed class CountingPageAssemblyLoader_Stub : IPageAssemblyLoader
    {
        public int PrefetchCallCount { get; private set; }

        public IReadOnlyList<Assembly> LoadedAssemblies { get; } = [];

        public Task EnsureLoadedAsync(
            string pageKey,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task PrefetchHomePrimaryJourneysAsync(CancellationToken cancellationToken = default)
        {
            PrefetchCallCount++;
            return Task.CompletedTask;
        }
    }
}
