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
    ///     Validates that when the Raindrop API returns HTTP 200 but the JSON
    ///     response lacks an "items" property, the function logs a warning and
    ///     returns the full JSON body as a fallback response.
    /// </summary>
    [Test]
    public async Task Should_Return_Full_Json_When_Items_Property_Missing()
    {
        // Arrange
        var logger = NullLogger<RaindropListVideos>.Instance;
        var settings = Options.Create(new Settings { RainDropTestToken = "test-token" });
        using var handler = new ControlledHttpHandler_Fake(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"not_items": "something_else"}""")
            }));
        var factory = new HttpClientFactory_Fake(handler);
        var function = new RaindropListVideos(logger, settings, factory);
        var functionContext = TestScope.CreateFunctionContext(nameof(RaindropListVideos));
        var request = TestScope.CreateHttpRequestData(functionContext);

        // Act
        using var response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = response.GetBodyAsString();
        await Assert.That(body).Contains("not_items");
    }

    /// <summary>
    ///     Validates that an <see cref="OperationCanceledException" /> during the HTTP
    ///     request propagates to the Functions host rather than being swallowed.
    /// </summary>
    [Test]
    public async Task Should_Throw_OperationCanceledException_When_Request_Is_Canceled()
    {
        // Arrange
        var logger = NullLogger<RaindropListVideos>.Instance;
        var settings = Options.Create(new Settings { RainDropTestToken = "test-token" });
        using var handler = new ControlledHttpHandler_Fake(_ =>
            throw new OperationCanceledException("Request canceled by host"));
        var factory = new HttpClientFactory_Fake(handler);
        var function = new RaindropListVideos(logger, settings, factory);
        var functionContext = TestScope.CreateFunctionContext(nameof(RaindropListVideos));
        var request = TestScope.CreateHttpRequestData(functionContext);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await function.RunAsync(request).ConfigureAwait(false));
    }

    /// <summary>
    ///     Validates that any unexpected exception is caught and converted into
    ///     an HTTP 500 response with the exception message. Parameterized with
    ///     different error messages to exercise the general exception handler
    ///     across multiple realistic failure messages.
    /// </summary>
    [Test]
    [Arguments("Simulated network failure")]
    [Arguments("DNS resolution timeout after 30s")]
    [Arguments("Invalid JSON in response body")]
    public async Task Should_Return_InternalServerError_When_Exception_Occurs(string errorMessage)
    {
        // Arrange
        var logger = NullLogger<RaindropListVideos>.Instance;
        var settings = Options.Create(new Settings { RainDropTestToken = "test-token" });
        using var handler = new ControlledHttpHandler_Fake(_ =>
            throw new HttpRequestException(errorMessage));
        var factory = new HttpClientFactory_Fake(handler);
        var function = new RaindropListVideos(logger, settings, factory);
        var functionContext = TestScope.CreateFunctionContext(nameof(RaindropListVideos));
        var request = TestScope.CreateHttpRequestData(functionContext);

        // Act
        using var response = (HttpResponseData_Mock)await function.RunAsync(request).ConfigureAwait(false);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        var body = response.GetBodyAsString();
        await Assert.That(body).Contains(errorMessage);
        JsonDocument.Parse(body); // Verify response is valid JSON even in error state
    }

    // ── Test infrastructure ────────────────────────────────────────────

    /// <summary>
    ///     Fakes <see cref="IHttpClientFactory" /> to return an HttpClient backed
    ///     by a controlled message handler.
    /// </summary>
    private sealed class HttpClientFactory_Fake(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>
    ///     Controlled <see cref="HttpMessageHandler" /> that delegates to a
    ///     configurable function for deterministic HTTP responses in tests.
    /// </summary>
    private sealed class ControlledHttpHandler_Fake : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public ControlledHttpHandler_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
