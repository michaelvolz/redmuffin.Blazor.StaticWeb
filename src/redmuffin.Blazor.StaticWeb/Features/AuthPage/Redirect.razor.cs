using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace redmuffin.Blazor.StaticWeb.Features.AuthPage;

/// <summary>
///     Redirect component for handling OAuth callback and token exchange.
/// </summary>
public partial class Redirect
{
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
    private ILogger<Redirect> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        LogOnInitializedAsyncStarted(Logger);
        _redirectUri = Navigation.BaseUri.TrimEnd('/') + "/redirect";
        LogRedirectUriSet(Logger, _redirectUri);

        var uri = new Uri(Navigation.Uri);
        LogCurrentUri(Logger, uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("code", out var codeVal))
        {
            await ProcessAuthorizationCodeAsync(codeVal.ToString()).ConfigureAwait(false);
        }
        else
        {
            LogNoCodeFoundInUrl(Logger);
            _error = "No authorization code found in URL.";
        }

        LogOnInitializedAsyncFinished(Logger);
    }

    private async Task ProcessAuthorizationCodeAsync(string authCode)
    {
        _authCode = authCode;
        LogAuthCodeFound(Logger, _authCode);
        try
        {
            LogAttemptingToSetRaindropAuthCode(Logger);
            await LocalStorage.SetItemAsync("raindrop_auth_code", _authCode).ConfigureAwait(false);
            LogRaindropAuthCodeSetSuccessfully(Logger);

            LogAttemptingToExchangeCode(Logger);
            await ExchangeCodeForTokenAsync(_authCode!).ConfigureAwait(false);
            LogExchangeCodeCompleted(Logger);
        }
        catch (Exception ex)
        {
            LogErrorDuringOnInitialized(Logger, ex);
            _error = $"Error during initialization: {ex.Message}";
        }
    }

    private async Task ExchangeCodeForTokenAsync(string code)
    {
        LogExchangeCodeStarted(Logger, code);
        var apiRequest = new ApiExchangeRequest { Code = code, RedirectUri = _redirectUri };

        // Use JsonSerializerContext for serialization to avoid trimming issues
        var jsonRequest = JsonSerializer.Serialize(apiRequest, ApiExchangeRequestContext.Default.ApiExchangeRequest);
        LogApiRequestJson(Logger, jsonRequest);
        using var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            LogPostingToExchangeApi(Logger);
            var response = await Http.PostAsync("/api/ExchangeRaindropCode", requestContent).ConfigureAwait(false);
            LogResponseReceived(Logger, response.StatusCode);

            if (response.IsSuccessStatusCode)
                await HandleSuccessfulTokenResponseAsync(response).ConfigureAwait(false);
            else
                await HandleFailedTokenResponseAsync(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogExceptionDuringTokenExchange(Logger, ex);
            _error = $"An exception occurred while exchanging token: {ex.Message}";
        }

        LogExchangeCodeFinished(Logger);
    }

    private async Task HandleSuccessfulTokenResponseAsync(HttpResponseMessage response)
    {
        LogSuccessfulResponseReceived(Logger);
        var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var apiResponse = await JsonSerializer.DeserializeAsync<ApiExchangeResponse>(
            responseStream, ApiExchangeRequestContext.Default.ApiExchangeResponse).ConfigureAwait(false);

        if (ParseAccessToken(apiResponse, out var token, out var error))
        {
            await StoreAccessTokenAsync(token).ConfigureAwait(false);
        }
        else
        {
            _error = error;
            LogFailedToRetrieveAccessToken(Logger, apiResponse?.Error);
        }
    }

    private async Task StoreAccessTokenAsync(string accessToken)
    {
        _accessToken = accessToken;
        LogAccessTokenRetrieved(Logger, _accessToken);
        try
        {
            LogAttemptingToSetAccessToken(Logger);
            await LocalStorage.SetItemAsync("raindrop_access_token", _accessToken).ConfigureAwait(false);
            LogAccessTokenSetSuccessfully(Logger);
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
        LogTokenExchangeFailed(Logger, response.StatusCode, errorContentString);
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

    public static bool ParseAccessToken(ApiExchangeResponse? response, out string token, out string? error)
    {
        if (!string.IsNullOrEmpty(response?.AccessToken))
        {
            token = response.AccessToken;
            error = null;
            return true;
        }

        token = string.Empty;
        error = response?.Error ?? "Failed to retrieve access token from API: No token in response.";
        return false;
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