using System.Net;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class ExchangeRaindropCodeFunction
{
    [LoggerMessage(1, LogLevel.Information, "ExchangeRaindropCode function processed a request.", EventName = nameof(Log_FunctionProcessedRequest))]
    public static partial void Log_FunctionProcessedRequest(ILogger logger);

    [LoggerMessage(2, LogLevel.Warning, "Request is null or code is missing.", EventName = nameof(Log_MissingCodeOrRequest))]
    public static partial void Log_MissingCodeOrRequest(ILogger logger);

    [LoggerMessage(11, LogLevel.Warning, "Request body is not valid JSON.", EventName = nameof(Log_InvalidJsonBody))]
    public static partial void Log_InvalidJsonBody(ILogger logger);

    [LoggerMessage(3, LogLevel.Warning, "Redirect URI is missing.", EventName = nameof(Log_MissingRedirectUri))]
    public static partial void Log_MissingRedirectUri(ILogger logger);

    [LoggerMessage(4, LogLevel.Information, "Request code: {Code}, Redirect URI: {RedirectUri}", EventName = nameof(Log_RequestDetails))]
    public static partial void Log_RequestDetails(ILogger logger, string code, string redirectUri);

    [LoggerMessage(5, LogLevel.Information, "Posting to Raindrop API.", EventName = nameof(Log_PostingToRaindropApi))]
    public static partial void Log_PostingToRaindropApi(ILogger logger);

    [LoggerMessage(6, LogLevel.Information, "Successfully received response from Raindrop API.", EventName = nameof(Log_SuccessfulApiResponse))]
    public static partial void Log_SuccessfulApiResponse(ILogger logger);

    [LoggerMessage(7, LogLevel.Information, "Access token retrieved successfully.", EventName = nameof(Log_AccessTokenRetrieved))]
    public static partial void Log_AccessTokenRetrieved(ILogger logger);

    [LoggerMessage(8, LogLevel.Warning, "No access_token in response from Raindrop API.", EventName = nameof(Log_NoAccessTokenInResponse))]
    public static partial void Log_NoAccessTokenInResponse(ILogger logger);

    [LoggerMessage(9, LogLevel.Warning, "Token request failed with status code: {StatusCode}. Response: {Response}", EventName = nameof(Log_TokenRequestFailed))]
    public static partial void Log_TokenRequestFailed(ILogger logger, HttpStatusCode statusCode, string response);

    [LoggerMessage(10, LogLevel.Error, "An error occurred while exchanging Raindrop code.", EventName = nameof(Log_ExchangeError))]
    public static partial void Log_ExchangeError(ILogger logger, Exception exception);
}
