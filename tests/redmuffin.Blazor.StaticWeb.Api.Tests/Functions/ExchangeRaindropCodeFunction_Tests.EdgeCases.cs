using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

[Category("Feature:Api")]
public sealed partial class ExchangeRaindropCodeFunction_Tests
{
    /// <summary>
    ///     Validates that the function returns BadRequest when code is missing from the request.
    /// </summary>
    [Test]
    public async Task Should_Return_BadRequest_When_Code_Is_Missing()
    {
        // Arrange
        using var scope = CreateTestScope();
        var logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        var settings = Options.Create(new Settings
        {
            RainDropClientId = "test-client-id",
            RainDropClientSecret = "test-client-secret"
        });
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var httpClientFactory = functionContext.InstanceServices.GetRequiredService<IHttpClientFactory>();
        var function = new ExchangeRaindropCodeFunction(logger, settings, httpClientFactory);

        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "", // Empty code
            RedirectUri = "http://localhost:5000/callback"
        };
        using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

        HttpResponseData_Mock? response = null;
        try
        {
            // Act
            response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            JsonDocument.Parse(responseBody); // Verify response is valid JSON

            await Assert.That(responseBody).Contains("Missing code");
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse) await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Validates that the function returns BadRequest when redirect_uri is missing from the request.
    /// </summary>
    [Test]
    public async Task Should_Return_BadRequest_When_RedirectUri_Is_Missing()
    {
        // Arrange
        using var scope = CreateTestScope();
        var logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        var settings = Options.Create(new Settings
        {
            RainDropClientId = "test-client-id",
            RainDropClientSecret = "test-client-secret"
        });
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var httpClientFactory = functionContext.InstanceServices.GetRequiredService<IHttpClientFactory>();
        var function = new ExchangeRaindropCodeFunction(logger, settings, httpClientFactory);

        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "valid-code",
            RedirectUri = null // Missing redirect_uri
        };
        using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

        HttpResponseData_Mock? response = null;
        try
        {
            // Act
            response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            JsonDocument.Parse(responseBody); // Verify response is valid JSON

            await Assert.That(responseBody).Contains("Missing redirect_uri");
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse) await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Validates that the function returns BadRequest when request body is null.
    /// </summary>
    [Test]
    public async Task Should_Return_BadRequest_When_Request_Body_Is_Null()
    {
        // Arrange
        using var scope = CreateTestScope();
        var logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        var settings = Options.Create(new Settings
        {
            RainDropClientId = "test-client-id",
            RainDropClientSecret = "test-client-secret"
        });
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var httpClientFactory = functionContext.InstanceServices.GetRequiredService<IHttpClientFactory>();
        var function = new ExchangeRaindropCodeFunction(logger, settings, httpClientFactory);

        using var request = TestScope.CreateHttpRequestData(functionContext, null); // Null body

        HttpResponseData_Mock? response = null;
        try
        {
            // Act
            response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            JsonDocument.Parse(responseBody); // Verify response is valid JSON

            await Assert.That(responseBody).Contains("Missing code");
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse) await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }
}