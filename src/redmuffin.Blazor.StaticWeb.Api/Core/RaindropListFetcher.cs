using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Core;

/// <summary>
///     Shared HTTP fetcher for Raindrop collection list endpoints.
///     Encapsulates the common logic of calling the Raindrop API,
///     parsing the JSON response, and producing an appropriate
///     <see cref="HttpResponseData" /> for every outcome.
/// </summary>
public static class RaindropListFetcher
{
    private const string BaseUrl = "https://api.raindrop.io/rest/v1/raindrops";

    public static async Task<HttpResponseData> FetchAsync(
        HttpRequestData request,
        string collectionId,
        string bearerToken,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        Action<ILogger, string> logFetch,
        Action<ILogger, Exception> logError,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var apiUrl = $"{BaseUrl}/{collectionId}?sort=-created";
            logFetch(logger, apiUrl);

            var response = await httpClient.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("items", out var itemsElement))
                {
                    var okResp = request.CreateResponse(HttpStatusCode.OK);
                    await okResp.WriteAsJsonAsync(itemsElement.Clone(), cancellationToken).ConfigureAwait(false);
                    return okResp;
                }

                var fallbackResp = request.CreateResponse(HttpStatusCode.OK);
                await fallbackResp.WriteAsJsonAsync(jsonDoc.RootElement.Clone(), cancellationToken).ConfigureAwait(false);
                return fallbackResp;
            }

            var errResp = request.CreateResponse(HttpStatusCode.BadGateway);
            await errResp.WriteAsJsonAsync(
                    new { Error = $"Raindrop API request failed: {response.StatusCode}", Details = json }, cancellationToken)
                .ConfigureAwait(false);
            return errResp;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logError(logger, ex);
            var errResp = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errResp.WriteAsJsonAsync(new { Error = ex.Message }, cancellationToken).ConfigureAwait(false);
            return errResp;
        }
    }
}
