using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;

#pragma warning disable CA1816, SA1204

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public partial class RaindropListArticles(ILogger<RaindropListArticles> logger, IOptions<Settings> settings, IHttpClientFactory httpClientFactory)
{
    private const string TargetCollectionId = "56658122";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly Settings _settings = settings.Value;

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

    /// <summary>
    ///     Handles HTTP GET requests to fetch a list of articles from the Raindrop API.
    /// </summary>
    /// <param name="request">The HTTP request data containing the trigger information.</param>
    /// <returns>
    ///     An <see cref="HttpResponseData" /> object containing the response data.
    ///     If successful, it includes the list of articles retrieved from the Raindrop API.
    ///     If an error occurs, it includes an appropriate error message.
    /// </returns>
    [Function("RaindropListArticles")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        Log_FunctionProcessed(logger);

        var token = request.FunctionContext.CancellationToken;

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.RainDropTestToken);

            var apiUrl = $"https://api.raindrop.io/rest/v1/raindrops/{TargetCollectionId}?sort=-created";
            Log_FetchArticles(logger, apiUrl);

            var response = await httpClient.GetAsync(apiUrl, token).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                Log_ResponseReceived(logger);
                using var jsonDoc = JsonDocument.Parse(json);
                var items = jsonDoc.RootElement.TryGetProperty("items", out var itemsElement) ? itemsElement.Clone() : jsonDoc.RootElement.Clone();

                var okResp = request.CreateResponse(HttpStatusCode.OK);
                await okResp.WriteAsJsonAsync(items, token).ConfigureAwait(false);
                return okResp;
            }

            Log_RequestFailed(logger, response.StatusCode, json);
            var errResp = request.CreateResponse(HttpStatusCode.BadRequest);
            await errResp.WriteAsJsonAsync(
                    new { Error = $"Raindrop API request failed: {response.StatusCode}", Details = json }, token)
                .ConfigureAwait(false);
            return errResp;
        }
        catch (OperationCanceledException)
        {
            Log_OperationCanceled(logger);
            throw;
        }
        catch (Exception ex)
        {
            Log_ErrorFetchingArticles(logger, ex);
            var errResp = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errResp.WriteAsJsonAsync(new { Error = ex.Message }, token).ConfigureAwait(false);
            return errResp;
        }
    }
}