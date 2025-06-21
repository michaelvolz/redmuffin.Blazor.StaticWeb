using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Redirect
{
	private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

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
		Logger.LogInformation("OnInitializedAsync started.");
		_redirectUri = Navigation.BaseUri.TrimEnd('/') + "/redirect";
		Logger.LogInformation("Redirect URI set to: {RedirectUri}", _redirectUri);

		var uri = new Uri(Navigation.Uri);
		Logger.LogInformation("Current URI: {Uri}", uri);
		var query = QueryHelpers.ParseQuery(uri.Query);

		if (query.TryGetValue("code", out var codeVal))
		{
			_authCode = codeVal.ToString();
			Logger.LogInformation("Authorization code found: {AuthCode}", _authCode);
			try
			{
				Logger.LogInformation("Attempting to set 'raindrop_auth_code' in LocalStorage.");
				await LocalStorage.SetItemAsync("raindrop_auth_code", _authCode);
				Logger.LogInformation("'raindrop_auth_code' successfully set in LocalStorage.");

				Logger.LogInformation("Attempting to exchange code for token.");
				await ExchangeCodeForTokenAsync(_authCode!);
				Logger.LogInformation("ExchangeCodeForTokenAsync completed.");
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error during OnInitializedAsync after obtaining auth code.");
				_error = $"Error during initialization: {ex.Message}";
			}
		}
		else
		{
			Logger.LogWarning("No 'code' found in URL query parameters.");
			_error = "No authorization code found in URL.";
		}

		Logger.LogInformation("OnInitializedAsync finished.");
	}

	private async Task ExchangeCodeForTokenAsync(string code)
	{
		Logger.LogInformation("ExchangeCodeForTokenAsync started with code: {Code}", code);
		var apiRequest = new ApiExchangeRequest { Code = code, RedirectUri = _redirectUri };

		// Use JsonSerializerContext for serialization to avoid trimming issues
		var jsonRequest = JsonSerializer.Serialize(apiRequest, ApiExchangeRequestContext.Default.ApiExchangeRequest);
		Logger.LogDebug("API Request JSON: {JsonRequest}", jsonRequest);
		var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

		try
		{
			Logger.LogInformation("Posting to /api/ExchangeRaindropCode");
			var response = await Http.PostAsync("/api/ExchangeRaindropCode", requestContent);
			Logger.LogInformation("Response received with status code: {StatusCode}", response.StatusCode);

			if (response.IsSuccessStatusCode)
			{
				Logger.LogInformation("Response was successful. Attempting to deserialize.");
				var responseStream = await response.Content.ReadAsStreamAsync();
				var apiResponse = await JsonSerializer.DeserializeAsync<ApiExchangeResponse>(
					responseStream, ApiExchangeRequestContext.Default.ApiExchangeResponse);
				Logger.LogDebug("API Response: {@ApiResponse}", apiResponse);

				if (!string.IsNullOrEmpty(apiResponse?.AccessToken))
				{
					_accessToken = apiResponse.AccessToken;
					Logger.LogInformation("Access token retrieved: {AccessToken}", _accessToken);
					try
					{
						Logger.LogInformation("Attempting to set 'raindrop_access_token' in LocalStorage.");
						await LocalStorage.SetItemAsync("raindrop_access_token", _accessToken);
						Logger.LogInformation("'raindrop_access_token' successfully set in LocalStorage.");
					}
					catch (Exception ex)
					{
						Logger.LogError(ex, "Error setting access token in LocalStorage.");
						_error = $"Error storing access token: {ex.Message}";
					}
				}
				else
				{
					_error = apiResponse?.Error ?? "Failed to retrieve access token from API: No token in response.";
					Logger.LogWarning("Failed to retrieve access token. API Error: {Error}", apiResponse?.Error);
				}
			}
			else
			{
				var errorContentString = await response.Content.ReadAsStringAsync();
				Logger.LogError("Token exchange with API failed. Status: {StatusCode}, Details: {ErrorContent}", response.StatusCode, errorContentString);
				ApiExchangeResponse? apiErrorResponse = null;
				try
				{
					apiErrorResponse = JsonSerializer.Deserialize<ApiExchangeResponse>(
						errorContentString, ApiExchangeRequestContext.Default.ApiExchangeResponse);
				}
				catch (JsonException jsonEx)
				{
					Logger.LogError(jsonEx, "Failed to deserialize error response from API.");
				}

				_error = apiErrorResponse?.Error ?? $"Token exchange with API failed: {response.StatusCode}. Details: {errorContentString}";
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "An exception occurred while exchanging token.");
			_error = $"An exception occurred while exchanging token: {ex.Message}";
		}

		Logger.LogInformation("ExchangeCodeForTokenAsync finished.");
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