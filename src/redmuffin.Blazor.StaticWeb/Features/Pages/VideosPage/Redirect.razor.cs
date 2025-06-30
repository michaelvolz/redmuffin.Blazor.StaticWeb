using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Redirect
{
	// LoggerMessage delegates for better performance
	private static readonly Action<ILogger, Exception?> LogOnInitializedAsyncStarted =
		LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogOnInitializedAsyncStarted)),
			"OnInitializedAsync started.");

	private static readonly Action<ILogger, string, Exception?> LogRedirectUriSet =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(LogRedirectUriSet)),
			"Redirect URI set to: {RedirectUri}");

	private static readonly Action<ILogger, Uri, Exception?> LogCurrentUri =
		LoggerMessage.Define<Uri>(LogLevel.Information, new EventId(3, nameof(LogCurrentUri)),
			"Current URI: {Uri}");

	private static readonly Action<ILogger, string, Exception?> LogAuthCodeFound =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, nameof(LogAuthCodeFound)),
			"Authorization code found: {AuthCode}");

	private static readonly Action<ILogger, Exception?> LogAttemptingToSetRaindropAuthCode =
		LoggerMessage.Define(LogLevel.Information, new EventId(5, nameof(LogAttemptingToSetRaindropAuthCode)),
			"Attempting to set 'raindrop_auth_code' in LocalStorage.");

	private static readonly Action<ILogger, Exception?> LogRaindropAuthCodeSetSuccessfully =
		LoggerMessage.Define(LogLevel.Information, new EventId(6, nameof(LogRaindropAuthCodeSetSuccessfully)),
			"'raindrop_auth_code' successfully set in LocalStorage.");

	private static readonly Action<ILogger, Exception?> LogAttemptingToExchangeCode =
		LoggerMessage.Define(LogLevel.Information, new EventId(7, nameof(LogAttemptingToExchangeCode)),
			"Attempting to exchange code for token.");

	private static readonly Action<ILogger, Exception?> LogExchangeCodeCompleted =
		LoggerMessage.Define(LogLevel.Information, new EventId(8, nameof(LogExchangeCodeCompleted)),
			"ExchangeCodeForTokenAsync completed.");

	private static readonly Action<ILogger, Exception> LogErrorDuringOnInitialized =
		LoggerMessage.Define(LogLevel.Error, new EventId(9, nameof(LogErrorDuringOnInitialized)),
			"Error during OnInitializedAsync after obtaining auth code.");

	private static readonly Action<ILogger, Exception?> LogNoCodeFoundInUrl =
		LoggerMessage.Define(LogLevel.Warning, new EventId(10, nameof(LogNoCodeFoundInUrl)),
			"No 'code' found in URL query parameters.");

	private static readonly Action<ILogger, Exception?> LogOnInitializedAsyncFinished =
		LoggerMessage.Define(LogLevel.Information, new EventId(11, nameof(LogOnInitializedAsyncFinished)),
			"OnInitializedAsync finished.");

	private static readonly Action<ILogger, string, Exception?> LogExchangeCodeStarted =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(12, nameof(LogExchangeCodeStarted)),
			"ExchangeCodeForTokenAsync started with code: {Code}");

	private static readonly Action<ILogger, string, Exception?> LogApiRequestJson =
		LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, nameof(LogApiRequestJson)),
			"API Request JSON: {JsonRequest}");

	private static readonly Action<ILogger, Exception?> LogPostingToExchangeApi =
		LoggerMessage.Define(LogLevel.Information, new EventId(14, nameof(LogPostingToExchangeApi)),
			"Posting to /api/ExchangeRaindropCode");

	private static readonly Action<ILogger, HttpStatusCode, Exception?> LogResponseReceived =
		LoggerMessage.Define<HttpStatusCode>(LogLevel.Information, new EventId(15, nameof(LogResponseReceived)),
			"Response received with status code: {StatusCode}");

	private static readonly Action<ILogger, Exception?> LogSuccessfulResponseReceived =
		LoggerMessage.Define(LogLevel.Information, new EventId(16, nameof(LogSuccessfulResponseReceived)),
			"Response was successful. Attempting to deserialize.");

	private static readonly Action<ILogger, string, Exception?> LogAccessTokenRetrieved =
		LoggerMessage.Define<string>(LogLevel.Information, new EventId(17, nameof(LogAccessTokenRetrieved)),
			"Access token retrieved: {AccessToken}");

	private static readonly Action<ILogger, Exception?> LogAttemptingToSetAccessToken =
		LoggerMessage.Define(LogLevel.Information, new EventId(18, nameof(LogAttemptingToSetAccessToken)),
			"Attempting to set 'raindrop_access_token' in LocalStorage.");

	private static readonly Action<ILogger, Exception?> LogAccessTokenSetSuccessfully =
		LoggerMessage.Define(LogLevel.Information, new EventId(19, nameof(LogAccessTokenSetSuccessfully)),
			"'raindrop_access_token' successfully set in LocalStorage.");

	private static readonly Action<ILogger, Exception> LogErrorSettingAccessToken =
		LoggerMessage.Define(LogLevel.Error, new EventId(20, nameof(LogErrorSettingAccessToken)),
			"Error setting access token in LocalStorage.");

	private static readonly Action<ILogger, string?, Exception?> LogFailedToRetrieveAccessToken =
		LoggerMessage.Define<string?>(LogLevel.Warning, new EventId(21, nameof(LogFailedToRetrieveAccessToken)),
			"Failed to retrieve access token. API Error: {Error}");

	private static readonly Action<ILogger, HttpStatusCode, string, Exception?> LogTokenExchangeFailed =
		LoggerMessage.Define<HttpStatusCode, string>(LogLevel.Error, new EventId(22, nameof(LogTokenExchangeFailed)),
			"Token exchange with API failed. Status: {StatusCode}, Details: {ErrorContent}");

	private static readonly Action<ILogger, Exception> LogFailedToDeserializeErrorResponse =
		LoggerMessage.Define(LogLevel.Error, new EventId(23, nameof(LogFailedToDeserializeErrorResponse)),
			"Failed to deserialize error response from API.");

	private static readonly Action<ILogger, Exception> LogExceptionDuringTokenExchange =
		LoggerMessage.Define(LogLevel.Error, new EventId(24, nameof(LogExceptionDuringTokenExchange)),
			"An exception occurred while exchanging token.");

	private static readonly Action<ILogger, Exception?> LogExchangeCodeFinished =
		LoggerMessage.Define(LogLevel.Information, new EventId(25, nameof(LogExchangeCodeFinished)),
			"ExchangeCodeForTokenAsync finished.");

	private string? _accessToken;
	private string? _authCode;
	private string? _error;
	private string? _redirectUri = string.Empty;

	[Inject]
	private NavigationManager Navigation { get; set; } = null!;

	[Inject]
	private ILocalStorageService LocalStorage { get; set; } = null!;

	[Inject]
	private HttpClient Http { get; set; } = null!;

	[Inject]
	private ILogger<Redirect> Logger { get; set; } = null!; // Added ILogger injection

	protected override async Task OnInitializedAsync()
	{
		LogOnInitializedAsyncStarted(Logger, null);
		_redirectUri = Navigation.BaseUri.TrimEnd('/') + "/redirect";
		LogRedirectUriSet(Logger, _redirectUri, null);

		var uri = new Uri(Navigation.Uri);
		LogCurrentUri(Logger, uri, null);
		var query = QueryHelpers.ParseQuery(uri.Query);

		if (query.TryGetValue("code", out var codeVal))
		{
			await ProcessAuthorizationCodeAsync(codeVal.ToString()).ConfigureAwait(false);
		}
		else
		{
			LogNoCodeFoundInUrl(Logger, null);
			_error = "No authorization code found in URL.";
		}

		LogOnInitializedAsyncFinished(Logger, null);
	}

	private async Task ProcessAuthorizationCodeAsync(string authCode)
	{
		_authCode = authCode;
		LogAuthCodeFound(Logger, _authCode, null);
		try
		{
			LogAttemptingToSetRaindropAuthCode(Logger, null);
			await LocalStorage.SetItemAsync("raindrop_auth_code", _authCode).ConfigureAwait(false);
			LogRaindropAuthCodeSetSuccessfully(Logger, null);

			LogAttemptingToExchangeCode(Logger, null);
			await ExchangeCodeForTokenAsync(_authCode!).ConfigureAwait(false);
			LogExchangeCodeCompleted(Logger, null);
		}
		catch (Exception ex)
		{
			LogErrorDuringOnInitialized(Logger, ex);
			_error = $"Error during initialization: {ex.Message}";
		}
	}

	private async Task ExchangeCodeForTokenAsync(string code)
	{
		LogExchangeCodeStarted(Logger, code, null);
		var apiRequest = new ApiExchangeRequest { Code = code, RedirectUri = _redirectUri };

		// Use JsonSerializerContext for serialization to avoid trimming issues
		var jsonRequest = JsonSerializer.Serialize(apiRequest, ApiExchangeRequestContext.Default.ApiExchangeRequest);
		LogApiRequestJson(Logger, jsonRequest, null);
		using var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

		try
		{
			LogPostingToExchangeApi(Logger, null);
			var response = await Http.PostAsync("/api/ExchangeRaindropCode", requestContent).ConfigureAwait(false);
			LogResponseReceived(Logger, response.StatusCode, null);

			if (response.IsSuccessStatusCode)
			{
				await HandleSuccessfulTokenResponseAsync(response).ConfigureAwait(false);
			}
			else
			{
				await HandleFailedTokenResponseAsync(response).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			LogExceptionDuringTokenExchange(Logger, ex);
			_error = $"An exception occurred while exchanging token: {ex.Message}";
		}

		LogExchangeCodeFinished(Logger, null);
	}

	private async Task HandleSuccessfulTokenResponseAsync(HttpResponseMessage response)
	{
		LogSuccessfulResponseReceived(Logger, null);
		var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		var apiResponse = await JsonSerializer.DeserializeAsync<ApiExchangeResponse>(
			responseStream, ApiExchangeRequestContext.Default.ApiExchangeResponse).ConfigureAwait(false);

		if (!string.IsNullOrEmpty(apiResponse?.AccessToken))
		{
			await StoreAccessTokenAsync(apiResponse.AccessToken).ConfigureAwait(false);
		}
		else
		{
			_error = apiResponse?.Error ?? "Failed to retrieve access token from API: No token in response.";
			LogFailedToRetrieveAccessToken(Logger, apiResponse?.Error, null);
		}
	}

	private async Task StoreAccessTokenAsync(string accessToken)
	{
		_accessToken = accessToken;
		LogAccessTokenRetrieved(Logger, _accessToken, null);
		try
		{
			LogAttemptingToSetAccessToken(Logger, null);
			await LocalStorage.SetItemAsync("raindrop_access_token", _accessToken).ConfigureAwait(false);
			LogAccessTokenSetSuccessfully(Logger, null);
		}
		catch (Exception ex)
		{
			LogErrorSettingAccessToken(Logger, ex);
			_error = $"Error storing access token: {ex.Message}";
		}
	}

	private async Task HandleFailedTokenResponseAsync(HttpResponseMessage response)
	{
		var errorContentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		LogTokenExchangeFailed(Logger, response.StatusCode, errorContentString, null);
		ApiExchangeResponse? apiErrorResponse = null;
		try
		{
			apiErrorResponse = JsonSerializer.Deserialize<ApiExchangeResponse>(
				errorContentString, ApiExchangeRequestContext.Default.ApiExchangeResponse);
		}
		catch (JsonException jsonEx)
		{
			LogFailedToDeserializeErrorResponse(Logger, jsonEx);
		}

		_error = apiErrorResponse?.Error ?? $"Token exchange with API failed: {response.StatusCode}. Details: {errorContentString}";
	}

	public class ApiExchangeRequest
	{
		public string Code { get; set; } = string.Empty;
		public string? RedirectUri { get; set; }
	}

	public class ApiExchangeResponse
	{
		public string? AccessToken { get; set; }
		public string? Error { get; set; }
	}

	[JsonSerializable(typeof(ApiExchangeRequest))]
	[JsonSerializable(typeof(ApiExchangeResponse))]
	public partial class ApiExchangeRequestContext : JsonSerializerContext;
}