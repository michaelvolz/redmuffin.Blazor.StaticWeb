using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;
using redmuffin.Blazor.StaticWeb.Tests.Support.Http;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

/// <summary>
///     Validates ExchangeRaindropCodeFunction Azure Function behavior and OAuth token exchange.
///     Ensures proper API integration, error handling, and JSON response formatting for token exchange.
/// </summary>
[Category("Feature:Api")]
public sealed partial class ExchangeRaindropCodeFunction_Tests
{
    /// <summary>
    ///     Validates that a rejected token exchange returns BadRequest with a valid JSON error body.
    /// </summary>
    [Test]
    public async Task Should_Return_BadRequest_When_Token_Exchange_Is_Rejected()
    {
        // Arrange
        var logger = NullLogger<ExchangeRaindropCodeFunction>.Instance;
        var settings = Options.Create(
            new Settings
            {
                RainDropClientId = "test-client-id",
                RainDropClientSecret = "test-client-secret",
            }
        );
        using var handler = new ControlledHttpHandler_Fake(_ =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{"error":"invalid_grant"}"""),
                }
            )
        );
        using var factory = new HttpClientFactory_Fake(handler);
        var function = new ExchangeRaindropCodeFunction(logger, settings, factory);
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));

        var requestBody = new ExchangeRaindropCodeFunction.ExchangeRequest
        {
            Code = "test-code-that-will-fail",
            RedirectUri = "http://localhost:5000/callback",
        };
        using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

        // Act
        using var response = (HttpResponseData_Mock)
            await function.RunAsync(request).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        var responseBody = response.GetBodyAsString();
        JsonDocument.Parse(responseBody); // Verify response is valid JSON

        await Assert.That(responseBody).Contains("Token request failed");
    }
}
