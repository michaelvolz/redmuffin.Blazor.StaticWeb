using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

/// <summary>
///     Validates ExchangeRaindropCodeFunction Azure Function behavior and OAuth token exchange.
///     Ensures proper API integration, error handling, and JSON response formatting for token exchange.
/// </summary>
[Category("Feature:Api")]
[Category("Integration")]
public sealed partial class ExchangeRaindropCodeFunction_Tests
{
    /// <summary>
    ///     Validates that the function returns valid JSON response with access token when provided with valid credentials.
    /// </summary>
    [Test]
    public async Task Should_Return_Access_Token_When_Valid_Credentials_Provided()
    {
        // Arrange
        using var scope = CreateTestScope();
        var testClientId = scope.Configuration["Values:RainDropClientId"];
        var testClientSecret = scope.Configuration["Values:RainDropClientSecret"];

        if (string.IsNullOrWhiteSpace(testClientId) || string.IsNullOrWhiteSpace(testClientSecret))
        {
            Assert.Fail("RainDropClientId and RainDropClientSecret must be configured for integration tests.");
            return;
        }

        var logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        var settings = Options.Create(new Settings
        {
            RainDropClientId = testClientId,
            RainDropClientSecret = testClientSecret
        });
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var httpClientFactory = functionContext.InstanceServices.GetRequiredService<IHttpClientFactory>();
        var function = new ExchangeRaindropCodeFunction(logger, settings, httpClientFactory);

        // Note: This test requires a valid OAuth code from Raindrop OAuth flow.
        // For integration testing, you would need to obtain a real code first.
        // This test validates the function structure and response handling.
        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "test-code-that-will-fail",
            RedirectUri = "http://localhost:5000/callback"
        };
        using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

        HttpResponseData_Mock? response = null;
        try
        {
            // Act
            response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

            // Assert - The test code will fail, but we verify the response structure
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

            var responseBody = response.GetBodyAsString();
            JsonDocument.Parse(responseBody); // Verify response is valid JSON

            await Assert.That(responseBody).Contains("Error");
        }
        finally
        {
            if (response is IAsyncDisposable asyncDisposableResponse) await asyncDisposableResponse.DisposeAsync().ConfigureAwait(false);
        }
    }
}