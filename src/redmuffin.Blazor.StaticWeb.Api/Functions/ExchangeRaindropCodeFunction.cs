using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public class ExchangeRaindropCodeFunction(ILogger<ExchangeRaindropCodeFunction> logger, IOptions<Settings> settings, IHttpClientFactory httpClientFactory)
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, Exception?> LogFunctionProcessedRequest =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogFunctionProcessedRequest)),
            "ExchangeRaindropCode function processed a request.");

    private static readonly Action<ILogger, Exception?> LogMissingCodeOrRequest =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, nameof(LogMissingCodeOrRequest)),
            "Request is null or code is missing.");

    private static readonly Action<ILogger, Exception?> LogMissingRedirectUri =
        LoggerMessage.Define(LogLevel.Warning, new EventId(3, nameof(LogMissingRedirectUri)),
            "Redirect URI is missing.");

    private static readonly Action<ILogger, string, string, Exception?> LogRequestDetails =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(4, nameof(LogRequestDetails)),
            "Request code: {Code}, Redirect URI: {RedirectUri}");

    private static readonly Action<ILogger, Exception?> LogPostingToRaindropApi =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(LogPostingToRaindropApi)),
            "Posting to Raindrop API.");

    private static readonly Action<ILogger, Exception?> LogSuccessfulApiResponse =
        LoggerMessage.Define(LogLevel.Information, new EventId(6, nameof(LogSuccessfulApiResponse)),
            "Successfully received response from Raindrop API.");

    private static readonly Action<ILogger, Exception?> LogAccessTokenRetrieved =
        LoggerMessage.Define(LogLevel.Information, new EventId(7, nameof(LogAccessTokenRetrieved)),
            "Access token retrieved successfully.");

    private static readonly Action<ILogger, Exception?> LogNoAccessTokenInResponse =
        LoggerMessage.Define(LogLevel.Warning, new EventId(8, nameof(LogNoAccessTokenInResponse)),
            "No access_token in response from Raindrop API.");

    private static readonly Action<ILogger, HttpStatusCode, string, Exception?> LogTokenRequestFailed =
        LoggerMessage.Define<HttpStatusCode, string>(LogLevel.Warning, new EventId(9, nameof(LogTokenRequestFailed)),
            "Token request failed with status code: {StatusCode}. Response: {Response}");

    private static readonly Action<ILogger, Exception> LogExchangeError =
        LoggerMessage.Define(LogLevel.Error, new EventId(10, nameof(LogExchangeError)),
            "An error occurred while exchanging Raindrop code.");

    private readonly Settings _settings = settings.Value;

    [Function("ExchangeRaindropCode")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        LogFunctionProcessedRequest(logger, null);
        var token = req.FunctionContext.CancellationToken;

        try
        {
            var request = await DeserializeRequestAsync(req, token).ConfigureAwait(false);
            if (request == null)
            {
                return await CreateBadRequestResponseAsync(req, "Missing code.", token).ConfigureAwait(false);
            }

            var redirectUri = ValidateRedirectUri(request.RedirectUri);
            if (redirectUri == null)
            {
                return await CreateBadRequestResponseAsync(req, "Missing redirect_uri.", token).ConfigureAwait(false);
            }

            LogRequestDetails(logger, request.Code, redirectUri, null);

            return await ExchangeCodeForTokenAsync(req, request.Code, redirectUri, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogExchangeError(logger, ex);
            var errResp = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = ex.Message }, token).ConfigureAwait(false);
            return errResp;
        }
    }

    private static async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData req, string error, CancellationToken token)
    {
        var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResp.WriteAsJsonAsync(new ExchangeResponse { Error = error }, token).ConfigureAwait(false);
        return badResp;
    }

    private async Task<ExchangeRequest?> DeserializeRequestAsync(HttpRequestData req, CancellationToken token)
    {
        var request = await JsonSerializer.DeserializeAsync<ExchangeRequest>(req.Body, cancellationToken: token).ConfigureAwait(false);
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            LogMissingCodeOrRequest(logger, null);
            return null;
        }

        return request;
    }

    private string? ValidateRedirectUri(string? redirectUri)
    {
        var uri = redirectUri ?? string.Empty;
        if (string.IsNullOrWhiteSpace(uri))
        {
            LogMissingRedirectUri(logger, null);
            return null;
        }

        return uri;
    }

    private async Task<HttpResponseData> ExchangeCodeForTokenAsync(HttpRequestData req, string code, string redirectUri, CancellationToken token)
    {
        var requestData = new
        {
            grant_type = "authorization_code",
            code = code,
            client_id = _settings.RainDropClientId,
            client_secret = _settings.RainDropClientSecret,
            redirect_uri = redirectUri,
        };

        var jsonPayload = JsonSerializer.Serialize(requestData);
        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        LogPostingToRaindropApi(logger, null);
        using var httpClient = httpClientFactory.CreateClient();
        // Use default handler (allow redirects)
        var response = await httpClient.PostAsync("https://raindrop.io/oauth/access_token", content, token).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return await HandleSuccessfulResponseAsync(req, json, token).ConfigureAwait(false);
        }
        else
        {
            return await HandleFailedResponseAsync(req, response.StatusCode, json, token).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseData> HandleSuccessfulResponseAsync(HttpRequestData req, string json, CancellationToken token)
    {
        LogSuccessfulApiResponse(logger, null);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElem))
        {
            var accessToken = tokenElem.GetString();
            LogAccessTokenRetrieved(logger, null);
            var okResp = req.CreateResponse(HttpStatusCode.OK);
            await okResp.WriteAsJsonAsync(new ExchangeResponse { AccessToken = accessToken }, token).ConfigureAwait(false);
            return okResp;
        }

        LogNoAccessTokenInResponse(logger, null);
        var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
        await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = "No access_token in response." }, token).ConfigureAwait(false);
        return errResp;
    }

    private async Task<HttpResponseData> HandleFailedResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string json, CancellationToken token)
    {
        LogTokenRequestFailed(logger, statusCode, json, null);
        var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
        await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = $"Token request failed: {statusCode}" }, token).ConfigureAwait(false);
        return errResp;
    }

    public class ExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? RedirectUri { get; set; }
    }

    public class ExchangeResponse
    {
        public string? AccessToken { get; set; }
        public string? Error { get; set; }
    }
}