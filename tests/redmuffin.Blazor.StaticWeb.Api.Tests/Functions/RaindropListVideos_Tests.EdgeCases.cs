using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;
using redmuffin.Blazor.StaticWeb.Api.Functions;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

[Category("Feature:Api")]
public sealed partial class RaindropListVideos_Tests
{
    /// <summary>
    ///     Validates that the function returns appropriate error response when provided with invalid authentication.
    /// </summary>
    [Test]
    public async Task Should_Return_Error_Response_When_Invalid_Token_Provided()
    {
        // Arrange
        using var scope = CreateTestScope();
        var logger = NullLogger<RaindropListVideos>.Instance;
        var invalidToken = "invalid-token";
        var settings = Options.Create(new Settings { RainDropTestToken = invalidToken });
        var functionContext = TestScope.CreateFunctionContext(nameof(RaindropListVideos));
        var httpClientFactory = functionContext.InstanceServices.GetRequiredService<IHttpClientFactory>();
        var function = new RaindropListVideos(logger, settings, httpClientFactory);
        var request = TestScope.CreateHttpRequestData(functionContext);

        HttpResponseData_Mock? response = null;
        try
        {
            // Act
            response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

            // Assert
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