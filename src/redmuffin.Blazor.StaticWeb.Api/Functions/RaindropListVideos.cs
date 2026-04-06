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

public sealed partial class RaindropListVideos(ILogger<RaindropListVideos> logger, IOptions<Settings> settings, IHttpClientFactory httpClientFactory)
{
    private const string TargetCollectionId = "56109697";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly Settings _settings = settings.Value;

    /// <summary>
    ///     Handles HTTP GET requests to fetch a list of videos from the Raindrop API.
    /// </summary>
    /// <param name="request">The HTTP request data containing the trigger information.</param>
    /// <returns>
    ///     An <see cref="HttpResponseData" /> object containing the response data.
    ///     If successful, it includes the list of videos retrieved from the Raindrop API.
    ///     If an error occurs, it includes an appropriate error message.
    /// </returns>
    [Function("RaindropListVideos")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        Log_FunctionProcessed(logger);

        var token = request.FunctionContext.CancellationToken;

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.RainDropTestToken);

            var apiUrl = $"https://api.raindrop.io/rest/v1/raindrops/{TargetCollectionId}?sort=-created";
            Log_FetchVideos(logger, apiUrl);

            var response = await httpClient.GetAsync(apiUrl, token).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                Log_ResponseReceived(logger);
                using var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("items", out var itemsElement))
                {
                    var okResp = request.CreateResponse(HttpStatusCode.OK);
                    await okResp.WriteAsJsonAsync(itemsElement.Clone(), token).ConfigureAwait(false);
                    return okResp;
                }

                // Fallback: API should always return 'items', but if not, log warning and return full response
                Log_MissingItemsProperty(logger);
                var fallbackResp = request.CreateResponse(HttpStatusCode.OK);
                await fallbackResp.WriteAsJsonAsync(jsonDoc.RootElement.Clone(), token).ConfigureAwait(false);
                return fallbackResp;
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
            Log_ErrorFetchingVideos(logger, ex);
            var errResp = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errResp.WriteAsJsonAsync(new { Error = ex.Message }, token).ConfigureAwait(false);
            return errResp;
        }
    }
}