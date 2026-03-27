using System.Net;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class RaindropListVideos
{
    [LoggerMessage(1, LogLevel.Information, "Videos function processed a request.", EventName = nameof(Log_FunctionProcessed))]
    public static partial void Log_FunctionProcessed(ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Fetching videos from Raindrop API: {ApiUrl}", EventName = nameof(Log_FetchVideos))]
    public static partial void Log_FetchVideos(ILogger logger, string apiUrl);

    [LoggerMessage(3, LogLevel.Information, "Successfully received response from Raindrop API.", EventName = nameof(Log_ResponseReceived))]
    public static partial void Log_ResponseReceived(ILogger logger);

    [LoggerMessage(4, LogLevel.Warning, "Raindrop API request failed with status code: {StatusCode}. Response: {Response}",
        EventName = nameof(Log_RequestFailed))]
    public static partial void Log_RequestFailed(ILogger logger, HttpStatusCode statusCode, string response);

    [LoggerMessage(5, LogLevel.Error, "An error occurred while fetching videos from Raindrop.", EventName = nameof(Log_ErrorFetchingVideos))]
    public static partial void Log_ErrorFetchingVideos(ILogger logger, Exception exception);

    [LoggerMessage(6, LogLevel.Information, "Operation was canceled.", EventName = nameof(Log_OperationCanceled))]
    public static partial void Log_OperationCanceled(ILogger logger);
}
