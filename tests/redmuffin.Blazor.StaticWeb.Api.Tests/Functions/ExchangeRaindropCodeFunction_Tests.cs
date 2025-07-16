using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Api.Tests.Helpers;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

public class ExchangeRaindropCodeFunction_Tests : TestBase, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRaindropCodeFunction> _logger;
    private readonly IOptions<Settings> _settings;
    private readonly TestHttpMessageHandler _testMessageHandler;

    public ExchangeRaindropCodeFunction_Tests()
    {
        _logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        _settings = Options.Create(new Settings
        {
            RainDropClientId = "test_client_id",
            RainDropClientSecret = "test_client_secret"
        });

        // Create a test message handler that we can control
        _testMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_testMessageHandler);
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);
#pragma warning disable CA2000 // Dispose objects before losing scope - _httpClient is disposed in DisposeAsync
        _httpClientFactory.CreateClient().Returns(_httpClient);
#pragma warning restore CA2000
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _testMessageHandler?.Dispose();
        GC.SuppressFinalize(this);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static TestHttpRequestDataWithBody CreateHttpRequestWithBody(object body)
    {
        var functionContext = new TestFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var jsonBody = JsonSerializer.Serialize(body);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
        return new TestHttpRequestDataWithBody(functionContext, bodyStream);
    }

    private void SetupHttpMock(HttpStatusCode statusCode, string jsonContent)
    {
        var httpResponseMessage = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _testMessageHandler.SetResponse(httpResponseMessage);
    }

    [Test]
    public async Task RunAsync_WithValidRequest_ReturnsOkWithAccessToken()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid_code",
            RedirectUri = "http://localhost/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        var apiResponse = new { access_token = "fake_access_token" };
        SetupHttpMock(HttpStatusCode.OK, JsonSerializer.Serialize(apiResponse));

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.AccessToken).IsEqualTo("fake_access_token");
            await Assert.That(exchangeResponse.Error).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WithMissingCode_ReturnsBadRequest()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "",
            RedirectUri = "http://localhost/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).IsEqualTo("Missing code.");
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WithMissingRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid_code",
            RedirectUri = ""
        };
        var request = CreateHttpRequestWithBody(requestBody);

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).IsEqualTo("Missing redirect_uri.");
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WithApiError_ReturnsBadRequest()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid_code",
            RedirectUri = "http://localhost/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        var apiError = new { error = "invalid_grant" };
        SetupHttpMock(HttpStatusCode.BadRequest, JsonSerializer.Serialize(apiError));

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).Contains("Token request failed");
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WithApiSuccessButMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid_code",
            RedirectUri = "http://localhost/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        var apiResponse = new { not_an_access_token = "some_value" };
        SetupHttpMock(HttpStatusCode.OK, JsonSerializer.Serialize(apiResponse));

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).IsEqualTo("No access_token in response.");
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WhenHttpClientThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid_code",
            RedirectUri = "http://localhost/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        _testMessageHandler.SetException(new HttpRequestException("Network error"));

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).Contains("Network error");
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_WithInvalidJsonRequest_ReturnsInternalServerError()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var functionContext = new TestFunctionContext(nameof(ExchangeRaindropCodeFunction));

        // Create request with invalid JSON
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes("{ invalid json }"));
        var request = new TestHttpRequestDataWithBody(functionContext, bodyStream);

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

            var responseBody = response.GetBodyAsString();
            var exchangeResponse = JsonSerializer.Deserialize<ExchangeRaindropCodeFunction.ExchangeResponse>(responseBody);

            await Assert.That(exchangeResponse).IsNotNull();
            await Assert.That(exchangeResponse!.Error).IsNotNull();
            await Assert.That(exchangeResponse.AccessToken).IsNull();
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task RunAsync_VerifiesCorrectApiCallToRaindrop()
    {
        // Arrange
        var function = new ExchangeRaindropCodeFunction(_logger, _settings, _httpClientFactory);
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "test_authorization_code",
            RedirectUri = "https://example.com/redirect"
        };
        var request = CreateHttpRequestWithBody(requestBody);

        var raindropResponse = new { access_token = "test_token" };
        var raindropJson = JsonSerializer.Serialize(raindropResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(raindropJson, Encoding.UTF8, "application/json")
        };

        _testMessageHandler.SetResponse(httpResponseMessage);

        TestHttpResponseData? response = null;
        try
        {
            // Act
            response = (TestHttpResponseData)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            var capturedRequest = _testMessageHandler.LastRequest;
            await Assert.That(capturedRequest).IsNotNull();
            await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Post);
            await Assert.That(capturedRequest.RequestUri!.ToString()).IsEqualTo("https://raindrop.io/oauth/access_token");
            await Assert.That(capturedRequest.Content!.Headers.ContentType!.MediaType).IsEqualTo("application/json");

            // Verify the JSON payload sent to Raindrop.io
            var requestContent = _testMessageHandler.LastRequestContent;
            await Assert.That(requestContent).IsNotNull();
            var sentPayload = JsonSerializer.Deserialize<JsonElement>(requestContent!);

            await Assert.That(sentPayload.GetProperty("grant_type").GetString()).IsEqualTo("authorization_code");
            await Assert.That(sentPayload.GetProperty("code").GetString()).IsEqualTo("test_authorization_code");
            await Assert.That(sentPayload.GetProperty("client_id").GetString()).IsEqualTo("test_client_id");
            await Assert.That(sentPayload.GetProperty("client_secret").GetString()).IsEqualTo("test_client_secret");
            await Assert.That(sentPayload.GetProperty("redirect_uri").GetString()).IsEqualTo("https://example.com/redirect");
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse)
                await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
            httpResponseMessage.Dispose();
        }
    }
}