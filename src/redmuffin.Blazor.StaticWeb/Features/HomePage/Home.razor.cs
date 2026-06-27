using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using redmuffin.Blazor.StaticWeb.Core.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Features.HomePage;

public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required ILogger<Home> Logger { get; set; }
    [Inject] public required IHttpClientFactory HttpClientFactory { get; set; }
    [Inject] public required IDelayProvider DelayProvider { get; set; }

    /// <summary>
    ///     Gets or sets the cascading parameter for theme configuration. Demonstrates cascading parameter functionality.
    /// </summary>
    [CascadingParameter(Name = "AppTheme")]
    public string AppTheme { get; set; } = "default";

    /// <summary>
    ///     Gets or sets the cascading parameter for user preferences. Demonstrates complex cascading parameter scenarios.
    /// </summary>
    [CascadingParameter(Name = "UserPreferences")]
    public IDictionary<string, object>? UserPreferences { get; set; }

    /// <summary>
    ///     Gets or sets the cascading parameter for authorization state. Demonstrates authorization integration.
    /// </summary>
    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>
    ///     Gets or sets the demo input value for form accessibility testing.
    /// </summary>
    public string DemoInputValue { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the status message for screen reader announcements.
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the alert message for important screen reader announcements.
    /// </summary>
    public string AlertMessage { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the current authenticated user display name.
    /// </summary>
    public string? CurrentUserName { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    ///     Gets the current theme class based on cascading parameter.
    /// </summary>
    /// <returns>The CSS class name corresponding to the current theme.</returns>
    public string GetThemeClass()
    {
        return AppTheme switch
        {
            "dark" => "theme-dark",
            "light" => "theme-light",
            "high-contrast" => "theme-high-contrast",
            _ => "theme-default"
        };
    }

    /// <summary>
    ///     Gets user preference value by key.
    /// </summary>
    /// <param name="key">The preference key.</param>
    /// <returns>The preference value if found; otherwise, null.</returns>
    public object? GetUserPreference(string key)
    {
        return UserPreferences?.TryGetValue(key, out var value) == true ? value : null;
    }

    protected override void OnInitialized()
    {
        LogOnInitializedCalled(Logger);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync().ConfigureAwait(false);
        LogOnParametersSetAsyncCalled(Logger);

        // Log cascading parameter changes
        LogCascadingParameterChanged(Logger, AppTheme);

        // Handle authentication state changes
        if (AuthenticationState != null)
            try
            {
#pragma warning disable VSTHRD003 // Calling method isn't async
#pragma warning disable MA0004 // Use ConfigureAwait(false)
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                var authState = await AuthenticationState;
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning restore MA0004 // Use ConfigureAwait(false)
#pragma warning restore VSTHRD003 // Calling method isn't async
                IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
                CurrentUserName = authState.User.Identity?.Name;
                LogAuthorizationStateChanged(Logger, IsAuthenticated);
            }
            catch (Exception ex)
            {
                // Handle potential authentication state exceptions gracefully
                LogAuthenticationFailure(Logger, ex);
                IsAuthenticated = false;
                CurrentUserName = null;
            }

        // Use configurable delay (0ms in tests, 100ms in production)
        await DelayProvider.DelayAsync(100).ConfigureAwait(false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
        if (firstRender)
            LogFirstRenderCalled(Logger);
        else
            LogSubsequentRenderCalled(Logger);
    }

    /// <summary>
    ///     Handles the primary button click with accessibility status updates.
    /// </summary>
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger);
        StatusMessage = "Processing API request...";
        StateHasChanged();

        using var client = HttpClientFactory.CreateClient();
        try
        {
            var response = await client.GetAsync("https://example.com").ConfigureAwait(false);
            LogApiCallStatus(Logger, (int)response.StatusCode);
            StatusMessage = $"API call completed with status: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            LogApiCallFailed(Logger, ex);
            AlertMessage = "API call failed. Please try again.";
        }
        finally
        {
            StateHasChanged();
            // Use configurable delay (0ms in tests, 3000ms in production)
            await DelayProvider.DelayAsync(3000).ConfigureAwait(false);
            StatusMessage = string.Empty;
            AlertMessage = string.Empty;
            StateHasChanged();
        }
    }

    /// <summary>
    ///     Handles form submission with accessibility feedback.
    /// </summary>
    private async Task HandleFormSubmitAsync()
    {
        LogFormSubmitted(Logger, DemoInputValue);

        if (string.IsNullOrWhiteSpace(DemoInputValue))
        {
            AlertMessage = "Please enter a value before submitting the form.";
        }
        else
        {
            StatusMessage = $"Form submitted successfully with value: {DemoInputValue}";
            DemoInputValue = string.Empty; // Clear form after submission
        }

        StateHasChanged();

        // Use configurable delay (0ms in tests, 3000ms in production)
        await DelayProvider.DelayAsync(3000).ConfigureAwait(false);
        StatusMessage = string.Empty;
        AlertMessage = string.Empty;
        StateHasChanged();
    }
}