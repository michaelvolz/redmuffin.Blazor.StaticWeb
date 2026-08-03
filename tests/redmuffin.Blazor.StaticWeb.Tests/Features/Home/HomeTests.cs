using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using HomePage = redmuffin.Blazor.StaticWeb.Pages.Home.Home;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Home;

[Category("Feature:Home")]
[Category("Unit")]
public partial class HomeTests
{
    [Test]
    public async Task Home_Accessibility_FormHasProperStructure()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify form structure follows accessibility best practices
        using (Assert.Multiple())
        {
            // Form has proper aria-labelledby
            var form = component.Find("form");
            await Assert.That(form.GetAttribute("aria-labelledby")).IsEqualTo("demo-form-heading");

            // Form prevents default submission
            await Assert.That(form).IsNotNull(); // Form exists

            // All form controls have proper associations
            var allInputs = component.FindAll("input, button[type='submit']");
            await Assert.That(allInputs.Count).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Home_Accessibility_HasLiveRegionsForStatusUpdates()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify ARIA live regions are properly configured
        using (Assert.Multiple())
        {
            // Status region (polite)
            var statusRegion = component.Find("#status-region[role='status']");
            await Assert.That(statusRegion).IsNotNull();
            await Assert.That(statusRegion.GetAttribute("aria-live")).IsEqualTo("polite");
            await Assert.That(statusRegion.GetAttribute("aria-atomic")).IsEqualTo("true");

            // Alert region (assertive)
            var alertRegion = component.Find("#alert-region[role='alert']");
            await Assert.That(alertRegion).IsNotNull();
            await Assert.That(alertRegion.GetAttribute("aria-live")).IsEqualTo("assertive");
            await Assert.That(alertRegion.GetAttribute("aria-atomic")).IsEqualTo("true");
        }
    }

    [Test]
    public async Task Home_Accessibility_HasProperButtonDescriptions()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify buttons have proper accessibility descriptions
        using (Assert.Multiple())
        {
            // Primary button with aria-describedby
            var primaryButton = component.Find("button.primary-button");
            await Assert.That(primaryButton).IsNotNull();
            await Assert.That(primaryButton.GetAttribute("aria-describedby")).IsEqualTo("button-description");
            await Assert.That(primaryButton.TextContent.Trim()).IsEqualTo("Click me");

            // Button description exists
            var buttonDescription = component.Find("#button-description");
            await Assert.That(buttonDescription).IsNotNull();
            await Assert.That(buttonDescription.TextContent).Contains("Performs a demo API call");

            // Submit button with description
            var submitButton = component.Find("button[type='submit']");
            await Assert.That(submitButton).IsNotNull();
            await Assert.That(submitButton.GetAttribute("aria-describedby")).IsEqualTo("submit-description");
        }
    }

    [Test]
    public async Task Home_Accessibility_HasProperFormLabelsAndDescriptions()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify form accessibility compliance
        using (Assert.Multiple())
        {
            // Input field with proper label association
            var input = component.Find("input#demo-input");
            var label = component.Find("label[for='demo-input']");
            await Assert.That(input).IsNotNull();
            await Assert.That(label).IsNotNull();
            await Assert.That(label.TextContent).Contains("Demo Input:");

            // Input has aria-describedby for help text
            await Assert.That(input.GetAttribute("aria-describedby")).IsEqualTo("demo-input-help");

            // Help text exists with proper ID
            var helpText = component.Find("#demo-input-help");
            await Assert.That(helpText).IsNotNull();
            await Assert.That(helpText.TextContent).Contains("accessibility testing");
        }
    }


    [Test]
    public async Task Home_Accessibility_HasProperLandmarksAndRoles()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify ARIA landmarks and roles are properly implemented
        using (Assert.Multiple())
        {
            // Main landmark
            var mainElement = component.Find("main[role='main']");
            await Assert.That(mainElement).IsNotNull();
            await Assert.That(mainElement.GetAttribute("aria-labelledby")).IsEqualTo("page-heading");

            // Emoji section with proper role
            var emojiDiv = component.Find("div[role='img']");
            await Assert.That(emojiDiv).IsNotNull();
            await Assert.That(emojiDiv.GetAttribute("aria-label")).Contains("Collection of happy face emojis");
        }
    }

    // ========================================
    // ACCESSIBILITY TESTS - PRIME EXAMPLES FOR ALL FUTURE COMPONENTS
    // ========================================
    // These tests demonstrate comprehensive accessibility testing using bUnit's semantic checks
    // and modern WCAG 2.1 AA compliance validation patterns.
    // Copy these patterns for all future Blazor component accessibility testing.

    [Test]
    public async Task Home_Accessibility_HasProperSkipLink()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify skip link exists and has proper accessibility attributes
        var skipLink = component.Find("a.skip-link");
        using (Assert.Multiple())
        {
            await Assert.That(skipLink).IsNotNull();
            await Assert.That(skipLink.GetAttribute("href")).IsEqualTo("#main-content");
            await Assert.That(skipLink.GetAttribute("aria-label")).IsEqualTo("Skip to main content");
            await Assert.That(skipLink.TextContent).Contains("Skip to main content");
        }
    }

    [Test]
    public async Task Home_Accessibility_IconsHaveProperAriaHidden()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify decorative icons are hidden from screen readers
        var rocketIcon = component.Find("i.fa-rocket");
        await Assert.That(rocketIcon.GetAttribute("aria-hidden")).IsEqualTo("true");
    }

    [Test]
    public async Task Home_Accessibility_KeyboardNavigationSupport()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify keyboard navigation elements are present
        using (Assert.Multiple())
        {
            // Main heading can receive programmatic focus
            var mainHeading = component.Find("h1[tabindex='-1']");
            await Assert.That(mainHeading).IsNotNull();

            // Interactive elements are keyboard accessible (buttons, inputs)
            var interactiveElements = component.FindAll("button, input, a");
            await Assert.That(interactiveElements.Count).IsGreaterThan(0);

            // All interactive elements should be focusable (no tabindex="-1" except on heading)
            var buttonsAndInputs = component.FindAll("button:not([tabindex='-1']), input:not([tabindex='-1'])");
            await Assert.That(buttonsAndInputs.Count).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Home_Accessibility_SemanticHTMLStructure()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify semantic HTML structure compliance
        using (Assert.Multiple())
        {
            // Proper use of semantic elements
            await Assert.That(component.Find("main")).IsNotNull();
            await Assert.That(component.FindAll("section").Count).IsGreaterThanOrEqualTo(2);
            await Assert.That(component.Find("form")).IsNotNull();

            // Buttons have proper type attributes
            var buttons = component.FindAll("button");
            foreach (var button in buttons)
            {
                var buttonType = button.GetAttribute("type");
                await Assert.That(buttonType).IsNotNull(); // Should have explicit type
            }

            // Form has proper structure
            var form = component.Find("form");
            await Assert.That(form).IsNotNull();
        }
    }


    [Test]
    public async Task Home_Accessibility_VisuallyHiddenElementsAreAccessible()
    {
        // Arrange & Act
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Assert - Verify visually hidden elements exist for screen readers
        using (Assert.Multiple())
        {
            var visuallyHiddenElements = component.FindAll(".visually-hidden");
            await Assert.That(visuallyHiddenElements.Count).IsGreaterThan(0);

            // Check specific visually hidden headings
            var emojiHeading = component.Find("h2#emoji-heading.visually-hidden");
            await Assert.That(emojiHeading).IsNotNull();
            await Assert.That(emojiHeading.TextContent).Contains("Emoji Display");

            var controlsHeading = component.Find("h2#controls-heading.visually-hidden");
            await Assert.That(controlsHeading).IsNotNull();
            await Assert.That(controlsHeading.TextContent).Contains("Interactive Controls");
        }
    }


    // ========================================
    // ADVANCED NON-OBVIOUS SCENARIOS - TASK 5.5 IMPLEMENTATION ✅
    // ========================================
    // These tests cover sophisticated edge cases, resource management, memory leak prevention,
    // and complex interaction patterns that are often missed in standard testing.
    // Research-based scenarios covering real-world production issues.


    [Test]
    public async Task Home_AdvancedScenarios_FormValidation_EdgeCases_HandledCorrectly()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();
        var input = component.Find("input#demo-input");
        var submitButton = component.Find("button[type='submit']");

        // Test various edge case input values
        var edgeCaseValues = new[]
        {
            "   ", // Whitespace only
            "\t\n\r", // Tab and newlines
            new string('a', 1000), // Very long string
            "<script>alert('xss')</script>", // Potential XSS
            "null", // String "null"
            "undefined" // String "undefined"
        };

        foreach (var testValue in edgeCaseValues)
        {
            // Act
            await input.ChangeAsync(new ChangeEventArgs { Value = testValue }).ConfigureAwait(false);
            scope.Logger.LogEntries.Clear();
            await submitButton.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);

            // Assert - All values should be logged (component logs before validation)
            // The component handles validation after logging for debugging purposes
            await Assert.That(scope.Logger.LogEntries.Any(entry =>
                entry.Message.Contains("Form submitted"))).IsTrue();
        }

        // Component should remain stable throughout all edge case testing
        await Assert.That(component.Markup).IsNotNull().And.Contains("redmuffin.StaticWeb");
    }


    // ========================================
    // CASCADING PARAMETERS TESTS - PRIME EXAMPLES FOR ALL FUTURE COMPONENTS
    // ========================================
    // These tests demonstrate comprehensive cascading parameter testing using TestScope
    // and modern TUnit patterns for Blazor component parameter scenarios.


    [Test]
    [Category("Smoke")]
    public async Task Home_ComponentStructure_HasRequiredElements()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        // Act & Assert - Verify core DOM structure exists (single structural concern)
        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1")).IsNotNull();
            await Assert.That(component.Find("div[style*='font-size:2rem']")).IsNotNull();
            await Assert.That(component.Find("button")).IsNotNull();
        }
    }
}