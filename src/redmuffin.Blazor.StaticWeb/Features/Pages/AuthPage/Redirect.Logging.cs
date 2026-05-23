using System.Net;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.AuthPage;

/// <summary>
///     LoggerMessage delegates for Redirect component.
/// </summary>
public partial class Redirect
{
    private static readonly Action<ILogger, Exception?> LogOnInitializedAsyncStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogOnInitializedAsyncStarted)),
            "OnInitializedAsync started.");

    private static readonly Action<ILogger, string, Exception?> LogRedirectUriSet =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(LogRedirectUriSet)),
            "Redirect URI set to: {RedirectUri}");

    private static readonly Action<ILogger, Uri, Exception?> LogCurrentUri =
        LoggerMessage.Define<Uri>(LogLevel.Information, new EventId(3, nameof(LogCurrentUri)),
            "Current URI: {Uri}");

    private static readonly Action<ILogger, string, Exception?> LogAuthCodeFound =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, nameof(LogAuthCodeFound)),
            "Authorization code found: {AuthCode}");

    private static readonly Action<ILogger, Exception?> LogAttemptingToSetRaindropAuthCode =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(LogAttemptingToSetRaindropAuthCode)),
            "Attempting to set 'raindrop_auth_code' in LocalStorage.");

    private static readonly Action<ILogger, Exception?> LogRaindropAuthCodeSetSuccessfully =
        LoggerMessage.Define(LogLevel.Information, new EventId(6, nameof(LogRaindropAuthCodeSetSuccessfully)),
            "'raindrop_auth_code' successfully set in LocalStorage.");

    private static readonly Action<ILogger, Exception?> LogAttemptingToExchangeCode =
        LoggerMessage.Define(LogLevel.Information, new EventId(7, nameof(LogAttemptingToExchangeCode)),
            "Attempting to exchange code for token.");

    private static readonly Action<ILogger, Exception?> LogExchangeCodeCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(8, nameof(LogExchangeCodeCompleted)),
            "ExchangeCodeForTokenAsync completed.");

    private static readonly Action<ILogger, Exception> LogErrorDuringOnInitialized =
        LoggerMessage.Define(LogLevel.Error, new EventId(9, nameof(LogErrorDuringOnInitialized)),
            "Error during OnInitializedAsync after obtaining auth code.");

    private static readonly Action<ILogger, Exception?> LogNoCodeFoundInUrl =
        LoggerMessage.Define(LogLevel.Warning, new EventId(10, nameof(LogNoCodeFoundInUrl)),
            "No 'code' found in URL query parameters.");

    private static readonly Action<ILogger, Exception?> LogOnInitializedAsyncFinished =
        LoggerMessage.Define(LogLevel.Information, new EventId(11, nameof(LogOnInitializedAsyncFinished)),
            "OnInitializedAsync finished.");

    private static readonly Action<ILogger, string, Exception?> LogExchangeCodeStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(12, nameof(LogExchangeCodeStarted)),
            "ExchangeCodeForTokenAsync started with code: {Code}");

    private static readonly Action<ILogger, string, Exception?> LogApiRequestJson =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, nameof(LogApiRequestJson)),
            "API Request JSON: {JsonRequest}");

    private static readonly Action<ILogger, Exception?> LogPostingToExchangeApi =
        LoggerMessage.Define(LogLevel.Information, new EventId(14, nameof(LogPostingToExchangeApi)),
            "Posting to /api/ExchangeRaindropCode");

    private static readonly Action<ILogger, HttpStatusCode, Exception?> LogResponseReceived =
        LoggerMessage.Define<HttpStatusCode>(LogLevel.Information, new EventId(15, nameof(LogResponseReceived)),
            "Response received with status code: {StatusCode}");

    private static readonly Action<ILogger, Exception?> LogSuccessfulResponseReceived =
        LoggerMessage.Define(LogLevel.Information, new EventId(16, nameof(LogSuccessfulResponseReceived)),
            "Response was successful. Attempting to deserialize.");

    private static readonly Action<ILogger, string, Exception?> LogAccessTokenRetrieved =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(17, nameof(LogAccessTokenRetrieved)),
            "Access token retrieved: {AccessToken}");

    private static readonly Action<ILogger, Exception?> LogAttemptingToSetAccessToken =
        LoggerMessage.Define(LogLevel.Information, new EventId(18, nameof(LogAttemptingToSetAccessToken)),
            "Attempting to set 'raindrop_access_token' in LocalStorage.");

    private static readonly Action<ILogger, Exception?> LogAccessTokenSetSuccessfully =
        LoggerMessage.Define(LogLevel.Information, new EventId(19, nameof(LogAccessTokenSetSuccessfully)),
            "'raindrop_access_token' successfully set in LocalStorage.");

    private static readonly Action<ILogger, Exception> LogErrorSettingAccessToken =
        LoggerMessage.Define(LogLevel.Error, new EventId(20, nameof(LogErrorSettingAccessToken)),
            "Error setting access token in LocalStorage.");

    private static readonly Action<ILogger, string?, Exception?> LogFailedToRetrieveAccessToken =
        LoggerMessage.Define<string?>(LogLevel.Warning, new EventId(21, nameof(LogFailedToRetrieveAccessToken)),
            "Failed to retrieve access token. API Error: {Error}");

    private static readonly Action<ILogger, HttpStatusCode, string, Exception?> LogTokenExchangeFailed =
        LoggerMessage.Define<HttpStatusCode, string>(LogLevel.Error, new EventId(22, nameof(LogTokenExchangeFailed)),
            "Token exchange with API failed. Status: {StatusCode}, Details: {ErrorContent}");

    private static readonly Action<ILogger, Exception> LogFailedToDeserializeErrorResponse =
        LoggerMessage.Define(LogLevel.Error, new EventId(23, nameof(LogFailedToDeserializeErrorResponse)),
            "Failed to deserialize error response from API.");

    private static readonly Action<ILogger, Exception> LogExceptionDuringTokenExchange =
        LoggerMessage.Define(LogLevel.Error, new EventId(24, nameof(LogExceptionDuringTokenExchange)),
            "An exception occurred while exchanging token.");

    private static readonly Action<ILogger, Exception?> LogExchangeCodeFinished =
        LoggerMessage.Define(LogLevel.Information, new EventId(25, nameof(LogExchangeCodeFinished)),
            "ExchangeCodeForTokenAsync finished.");
}