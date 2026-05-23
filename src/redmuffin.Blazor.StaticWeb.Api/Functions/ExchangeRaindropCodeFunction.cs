using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class ExchangeRaindropCodeFunction(ILogger<ExchangeRaindropCodeFunction> logger, IOptions<Settings> settings, IHttpClientFactory httpClientFactory)
{
    private readonly Settings _settings = settings.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private static async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData req, string error, CancellationToken token)
    {
        var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResp.WriteAsJsonAsync(new ExchangeResponse { Error = error }, token).ConfigureAwait(false);
        return badResp;
    }

    [Function("ExchangeRaindropCode")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        Log_FunctionProcessedRequest(logger);
        var token = req.FunctionContext.CancellationToken;

        try
        {
            var request = await DeserializeRequestAsync(req, token).ConfigureAwait(false);
            if (request is null) return await CreateBadRequestResponseAsync(req, "Missing code.", token).ConfigureAwait(false);

            var redirectUri = GetRedirectUriOrNull(request.RedirectUri);
            if (redirectUri is null) return await CreateBadRequestResponseAsync(req, "Missing redirect_uri.", token).ConfigureAwait(false);

            Log_RequestDetails(logger, request.Code, redirectUri);

            return await ExchangeCodeForTokenAsync(req, request.Code, redirectUri, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log_ExchangeError(logger, ex);
            var errResp = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = ex.Message }, token).ConfigureAwait(false);
            return errResp;
        }
    }

    private async Task<ExchangeRequest?> DeserializeRequestAsync(HttpRequestData req, CancellationToken token)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<ExchangeRequest>(req.Body, cancellationToken: token).ConfigureAwait(false);
            if (request is null || string.IsNullOrWhiteSpace(request.Code))
            {
                Log_MissingCodeOrRequest(logger);
                return null;
            }

            return request;
        }
        catch (JsonException)
        {
            Log_InvalidJsonBody(logger);
            return null;
        }
    }

    private string? GetRedirectUriOrNull(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            Log_MissingRedirectUri(logger);
            return null;
        }

        return redirectUri;
    }

    private async Task<HttpResponseData> ExchangeCodeForTokenAsync(HttpRequestData req, string code, string redirectUri, CancellationToken token)
    {
        var requestData = new
        {
            grant_type = "authorization_code",
            code,
            client_id = _settings.RainDropClientId,
            client_secret = _settings.RainDropClientSecret,
            redirect_uri = redirectUri
        };

        var jsonPayload = JsonSerializer.Serialize(requestData);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        Log_PostingToRaindropApi(logger);
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync("https://raindrop.io/oauth/access_token", content, token).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (response.IsSuccessStatusCode) return await HandleSuccessfulResponseAsync(req, json, token).ConfigureAwait(false);

        return await HandleFailedResponseAsync(req, response.StatusCode, json, token).ConfigureAwait(false);
    }

    private async Task<HttpResponseData> HandleSuccessfulResponseAsync(HttpRequestData req, string json, CancellationToken token)
    {
        Log_SuccessfulApiResponse(logger);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElem))
        {
            var accessToken = tokenElem.GetString();
            Log_AccessTokenRetrieved(logger);
            var okResp = req.CreateResponse(HttpStatusCode.OK);
            await okResp.WriteAsJsonAsync(new ExchangeResponse { AccessToken = accessToken }, token).ConfigureAwait(false);
            return okResp;
        }

        Log_NoAccessTokenInResponse(logger);
        var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
        await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = "No access_token in response." }, token).ConfigureAwait(false);
        return errResp;
    }

    private async Task<HttpResponseData> HandleFailedResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string json, CancellationToken token)
    {
        Log_TokenRequestFailed(logger, statusCode, json);
        // Preserve the original status code from the Raindrop API for proper error propagation
        var errResp = req.CreateResponse(statusCode);
        await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = $"Token request failed: {statusCode}" }, token).ConfigureAwait(false);
        return errResp;
    }

    public sealed class ExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? RedirectUri { get; set; }
    }

    public sealed class ExchangeResponse
    {
        public string? AccessToken { get; set; }
        public string? Error { get; set; }
    }
}
