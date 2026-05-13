using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class RaindropListArticles
{
    [LoggerMessage(1, LogLevel.Information, "Articles function processed a request.", EventName = nameof(Log_FunctionProcessed))]
    public static partial void Log_FunctionProcessed(ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Fetching articles from Raindrop API: {ApiUrl}", EventName = nameof(Log_FetchArticles))]
    public static partial void Log_FetchArticles(ILogger logger, string apiUrl);

    [LoggerMessage(5, LogLevel.Error, "An error occurred while fetching articles from Raindrop.", EventName = nameof(Log_ErrorFetchingArticles))]
    public static partial void Log_ErrorFetchingArticles(ILogger logger, Exception exception);
}
