namespace redmuffin.Blazor.StaticWeb.Features.HomePage;

public partial class Home
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "OnInitialized called")]
    private static partial void LogOnInitializedCalled(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "OnParametersSetAsync called")]
    private static partial void LogOnParametersSetAsyncCalled(ILogger logger);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "First render: OnAfterRenderAsync called")]
    private static partial void LogFirstRenderCalled(ILogger logger);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Subsequent render: OnAfterRenderAsync called")]
    private static partial void LogSubsequentRenderCalled(ILogger logger);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Button clicked")]
    private static partial void LogButtonClicked(ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Dummy API call status: {StatusCode}")]
    private static partial void LogApiCallStatus(ILogger logger, string statusCode);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Dummy API call failed")]
    private static partial void LogApiCallFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Form submitted with value: {InputValue}")]
    private static partial void LogFormSubmitted(ILogger logger, string inputValue);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Cascading parameter changed: {ParameterName}")]
    private static partial void LogCascadingParameterChanged(ILogger logger, string parameterName);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Authorization state changed: {IsAuthenticated}")]
    private static partial void LogAuthorizationStateChanged(ILogger logger, string isAuthenticated);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve authentication state")]
    private static partial void LogAuthenticationFailure(ILogger logger, Exception exception);
}
