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
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly Settings _settings = settings.Value;

	[Function("ExchangeRaindropCode")]
	public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
	{
		logger.LogInformation("ExchangeRaindropCode function processed a request.");

		var token = req.FunctionContext.CancellationToken;

		var request = await JsonSerializer.DeserializeAsync<ExchangeRequest>(req.Body, cancellationToken: token).ConfigureAwait(false);
		if (request == null || string.IsNullOrWhiteSpace(request.Code))
		{
			logger.LogWarning("Request is null or code is missing.");
			var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResp.WriteAsJsonAsync(new ExchangeResponse { Error = "Missing code." }, token).ConfigureAwait(false);
			return badResp;
		}

		var redirectUri = request.RedirectUri ?? string.Empty;
		if (string.IsNullOrWhiteSpace(redirectUri))
		{
			logger.LogWarning("Redirect URI is missing.");
			// Optionally set a default or require it
			var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResp.WriteAsJsonAsync(new ExchangeResponse { Error = "Missing redirect_uri." }, token).ConfigureAwait(false);
			return badResp;
		}

		logger.LogInformation("Request code: {Code}, Redirect URI: {RedirectUri}", request.Code, redirectUri);

		var content = new StringContent(
			$"grant_type=authorization_code&code={request.Code}&client_id={_settings.RainDropClientId}&client_secret={_settings.RainDropClientSecret}&redirect_uri={Uri.EscapeDataString(redirectUri)}",
			Encoding.UTF8, "application/x-www-form-urlencoded");
		try
		{
			logger.LogInformation("Posting to Raindrop API.");
			using HttpClient httpClient = _httpClientFactory.CreateClient();
			var response = await httpClient.PostAsync("https://raindrop.io/oauth/access_token", content, token).ConfigureAwait(false);
			var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				logger.LogInformation("Successfully received response from Raindrop API.");
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("access_token", out var tokenElem))
				{
					var accessToken = tokenElem.GetString();
					logger.LogInformation("Access token retrieved successfully.");
					var okResp = req.CreateResponse(HttpStatusCode.OK);
					await okResp.WriteAsJsonAsync(new ExchangeResponse { AccessToken = accessToken }, token).ConfigureAwait(false);
					return okResp;
				}

				logger.LogWarning("No access_token in response from Raindrop API.");
				var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
				await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = "No access_token in response." }, token).ConfigureAwait(false);
				return errResp;
			}
			else
			{
				logger.LogWarning("Token request failed with status code: {StatusCode}. Response: {Response}", response.StatusCode, json);
				var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
				await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = $"Token request failed: {response.StatusCode}" }, token).ConfigureAwait(false);
				return errResp;
			}
		}
	catch (Exception ex)
	{
		logger.LogError(ex, "An error occurred while exchanging Raindrop code.");
		var errResp = req.CreateResponse(HttpStatusCode.InternalServerError);
		await errResp.WriteAsJsonAsync(new ExchangeResponse { Error = ex.Message }, token).ConfigureAwait(false);
		return errResp;
	}
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