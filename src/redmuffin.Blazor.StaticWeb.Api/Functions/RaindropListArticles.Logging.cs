using System.Net;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class RaindropListArticles
{
    [LoggerMessage(1, LogLevel.Information, "Articles function processed a request.", EventName = nameof(Log_FunctionProcessed))]
    public static partial void Log_FunctionProcessed(ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Fetching articles from Raindrop API: {ApiUrl}", EventName = nameof(Log_FetchArticles))]
    public static partial void Log_FetchArticles(ILogger logger, string apiUrl);

    [LoggerMessage(3, LogLevel.Information, "Successfully received response from Raindrop API.", EventName = nameof(Log_ResponseReceived))]
    public static partial void Log_ResponseReceived(ILogger logger);

    [LoggerMessage(4, LogLevel.Warning, "Raindrop API request failed with status code: {StatusCode}. Response: {Response}",
        EventName = nameof(Log_RequestFailed))]
    public static partial void Log_RequestFailed(ILogger logger, HttpStatusCode statusCode, string response);

    [LoggerMessage(5, LogLevel.Error, "An error occurred while fetching articles from Raindrop.", EventName = nameof(Log_ErrorFetchingArticles))]
    public static partial void Log_ErrorFetchingArticles(ILogger logger, Exception exception);

    [LoggerMessage(6, LogLevel.Information, "Operation was canceled.", EventName = nameof(Log_OperationCanceled))]
    public static partial void Log_OperationCanceled(ILogger logger);

    [LoggerMessage(7, LogLevel.Warning, "Raindrop API response missing 'items' property, returning full response.", EventName = nameof(Log_MissingItemsProperty))]
    public static partial void Log_MissingItemsProperty(ILogger logger);
}
