using System.Net;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.AuthPage;

public partial class Redirect
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "OnInitializedAsync started.")]
    private static partial void LogOnInitializedAsyncStarted(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Redirect URI set to: {RedirectUri}")]
    private static partial void LogRedirectUriSet(ILogger logger, string redirectUri);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Current URI: {Uri}")]
    private static partial void LogCurrentUri(ILogger logger, Uri uri);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Authorization code found: {AuthCode}")]
    private static partial void LogAuthCodeFound(ILogger logger, string authCode);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Attempting to set 'raindrop_auth_code' in LocalStorage.")]
    private static partial void LogAttemptingToSetRaindropAuthCode(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "'raindrop_auth_code' successfully set in LocalStorage.")]
    private static partial void LogRaindropAuthCodeSetSuccessfully(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Attempting to exchange code for token.")]
    private static partial void LogAttemptingToExchangeCode(ILogger logger);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "ExchangeCodeForTokenAsync completed.")]
    private static partial void LogExchangeCodeCompleted(ILogger logger);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error during OnInitializedAsync after obtaining auth code.")]
    private static partial void LogErrorDuringOnInitialized(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "No 'code' found in URL query parameters.")]
    private static partial void LogNoCodeFoundInUrl(ILogger logger);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "OnInitializedAsync finished.")]
    private static partial void LogOnInitializedAsyncFinished(ILogger logger);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "ExchangeCodeForTokenAsync started with code: {Code}")]
    private static partial void LogExchangeCodeStarted(ILogger logger, string code);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "API Request JSON: {JsonRequest}")]
    private static partial void LogApiRequestJson(ILogger logger, string jsonRequest);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Posting to /api/ExchangeRaindropCode")]
    private static partial void LogPostingToExchangeApi(ILogger logger);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Response received with status code: {StatusCode}")]
    private static partial void LogResponseReceived(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "Response was successful. Attempting to deserialize.")]
    private static partial void LogSuccessfulResponseReceived(ILogger logger);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Access token retrieved: {AccessToken}")]
    private static partial void LogAccessTokenRetrieved(ILogger logger, string accessToken);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Attempting to set 'raindrop_access_token' in LocalStorage.")]
    private static partial void LogAttemptingToSetAccessToken(ILogger logger);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "'raindrop_access_token' successfully set in LocalStorage.")]
    private static partial void LogAccessTokenSetSuccessfully(ILogger logger);

    [LoggerMessage(EventId = 20, Level = LogLevel.Error, Message = "Error setting access token in LocalStorage.")]
    private static partial void LogErrorSettingAccessToken(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "Failed to retrieve access token. API Error: {Error}")]
    private static partial void LogFailedToRetrieveAccessToken(ILogger logger, string? error);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "Token exchange with API failed. Status: {StatusCode}, Details: {ErrorContent}")]
    private static partial void LogTokenExchangeFailed(ILogger logger, HttpStatusCode statusCode, string errorContent);

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Failed to deserialize error response from API.")]
    private static partial void LogFailedToDeserializeErrorResponse(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 24, Level = LogLevel.Error, Message = "An exception occurred while exchanging token.")]
    private static partial void LogExceptionDuringTokenExchange(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 25, Level = LogLevel.Information, Message = "ExchangeCodeForTokenAsync finished.")]
    private static partial void LogExchangeCodeFinished(ILogger logger);
}
