namespace redmuffin.Blazor.StaticWeb.Features.Pages.HomePage;

public partial class Home
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Exception?> LogOnInitializedCalled =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogOnInitializedCalled)),
            "OnInitialized called");

    private static readonly Action<ILogger, Exception?> LogOnParametersSetAsyncCalled =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogOnParametersSetAsyncCalled)),
            "OnParametersSetAsync called");

    private static readonly Action<ILogger, Exception?> LogFirstRenderCalled =
        LoggerMessage.Define(LogLevel.Information, new EventId(3, nameof(LogFirstRenderCalled)),
            "First render: OnAfterRenderAsync called");

    private static readonly Action<ILogger, Exception?> LogSubsequentRenderCalled =
        LoggerMessage.Define(LogLevel.Information, new EventId(4, nameof(LogSubsequentRenderCalled)),
            "Subsequent render: OnAfterRenderAsync called");

    private static readonly Action<ILogger, Exception?> LogButtonClicked =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(LogButtonClicked)),
            "Button clicked");

    private static readonly Action<ILogger, string, Exception?> LogApiCallStatus =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6, nameof(LogApiCallStatus)),
            "Dummy API call status: {StatusCode}");

    private static readonly Action<ILogger, Exception> LogApiCallFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(7, nameof(LogApiCallFailed)),
            "Dummy API call failed");

    private static readonly Action<ILogger, string, Exception?> LogFormSubmitted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(8, nameof(LogFormSubmitted)),
            "Form submitted with value: {InputValue}");

    private static readonly Action<ILogger, string, Exception?> LogCascadingParameterChanged =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(9, nameof(LogCascadingParameterChanged)),
            "Cascading parameter changed: {ParameterName}");

    private static readonly Action<ILogger, string, Exception?> LogAuthorizationStateChanged =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(10, nameof(LogAuthorizationStateChanged)),
            "Authorization state changed: {IsAuthenticated}");

    private static readonly Action<ILogger, Exception> LogAuthenticationFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(11, nameof(LogAuthenticationFailure)),
            "Failed to retrieve authentication state");
}